* ==============================================================================
* GAMS Model: Otimizacao de Corte de Bobinas - Beontag
* Versao: 24.0 - GEEK (Padrão Dummy Inteligente)
* Autor: GAMS Geek (Gemini AI)
* Data: 10 de Agosto de 2025
*
* DESCRICAO:
* Versao final que resolve o erro de inicializacao ($66) usando um
* padrao "dummy" (fantasma).
* 1. Um padrao 'pt_dummy' com rendimento e custo zero e criado e ativado
* antes do loop para permitir que o primeiro SOLVE execute.
* 2. Os duais gerados sao um reflexo puro da demanda, guiando o subproblema
* a criar o primeiro padrao real de forma otima.
* 3. O padrao dummy e desativado apos a criacao do primeiro padrao real.
* ==============================================================================

$TITLE Otimizacao de Corte de Bobinas - Beontag (v24.0 - Padrao Dummy)

* ------------------------------------------------------------------------------
* FASE 0: OPCOES GLOBAIS
* ------------------------------------------------------------------------------
$onDotL

* --- Opcoes do Solver
Option optcr  = 0.05;
*Option reslim = 3600;
Option solPrint = off;
Option limRow = 0, limCol = 0;

* --- Opcoes especificas para o CPLEX
$onecho > cplex.opt
mipemphasis 1
threads 0
heurfreq 5
$offecho

* ==============================================================================
* FASE 1: CARGA DE DADOS E DEFINICAO DE ESTRUTURAS
* ==============================================================================
$INCLUDE 'GamsInputData.dat'

* ------------------------------------------------------------------------------
* 1.2 Declaracao de Conjuntos e Parametros Adicionais
* ------------------------------------------------------------------------------
Sets
    pt       'Universo de padroes de corte potenciais' /pt_dummy, Job001*Job999/;

Alias (t, tt, t2, t_prod);
Alias (pt, pt_new);

* --- Parametros para calculo da capacidade ---
Parameter p_velocidadeEfetiva(j) 'pol/min efetivo';
p_velocidadeEfetiva(j) = p_velocidadeMaq(j) * p_eficiencia(j);

Parameter p_tempoProcesso(j) 'min por bobina-mae';
p_tempoProcesso(j)$p_velocidadeEfetiva(j) = s_comprimentoMae_pes / p_velocidadeEfetiva(j);

Parameter p_tempoUsoMaquina(j) 'min por uso na maquina j (setup + processo)';
p_tempoUsoMaquina(j) = p_tempoSetupBase(j) + p_tempoProcesso(j);

* --- Parametros de Custo e Qualidade ---
Scalar s_pesoCustoProducao   'Peso do custo de producao (por bobina-mae usada)' / 1 /;
Scalar W_backlog             'Peso gigante para penalizar o backlog no objetivo' /1e6/;
Scalar s_pesoCustoRefugo     'Custo de refugo (sobra de largura), para desempate' / 0.1 /;
Scalar s_minUtilPct_master   'Utilizacao minima para um padrao ser aceito pelo mestre' / 0.97 /;

Parameters
    p_precoDual(p_base,w,c)         'Preco dual da demanda (para pricing)'
    p_refugoLargura(pt)             'Largura nao utilizada (refugo) do padrao pt';

* Mapeamento de dados dos produtos
Parameters
    p_larguraPwC(p_base,w,c)    'Largura numerica do produto'
    p_compPwC(p_base,w,c)       'Comprimento numerico do produto';
p_larguraPwC(p_base,w,c)$(p_larguraProduto(p_base,w,c)) = p_larguraProduto(p_base,w,c);
p_compPwC(p_base,w,c)$(p_comprimentoProduto(p_base,w,c)) = p_comprimentoProduto(p_base,w,c);

* ==============================================================================
* FASE 2: DEFINICAO DO PROBLEMA MESTRE E SUBPROBLEMA
* ==============================================================================

Parameters
    p_rendimentoPadrao(pt,p_base,w,c) 'Qtd do produto (p,w,c) por padrao pt';
Set pt_on(pt) 'Padroes ativos que podem ser usados pelo mestre';

* --- Variaveis e Equacoes do Mestre ---
Variables
    v_usaPadrao(pt, j, t)
    Bcum(p_base,w,c,t)
    v_custoProducao
    v_somaBacklog
    zTotal;

Integer Variable v_usaPadrao;
Positive Variable Bcum;

Equations
    eq_capacidade(j, t)
    eq_backlogCum(p_base,w,c,t)
    eq_calcula_custo
    eq_calcula_backlog
    eq_obj_lex;

eq_capacidade(j,t)$(p_tempoDisponivel(j,t) > 0)..
    sum(pt$pt_on(pt), v_usaPadrao(pt,j,t) * p_tempoUsoMaquina(j))
    =l= p_tempoDisponivel(j,t);

eq_backlogCum(p_base,w,c,t)..
    Bcum(p_base,w,c,t) =g=
        sum(t2$(ord(t2) <= ord(t)), p_demanda(p_base,w,c,t2))
      - sum((pt,j,t2)$(ord(t2) <= ord(t)), p_rendimentoPadrao(pt,p_base,w,c) * v_usaPadrao(pt,j,t2));

eq_calcula_custo..
    v_custoProducao =e=
        s_pesoCustoProducao * sum((pt,j,t)$pt_on(pt), v_usaPadrao(pt,j,t))
      + s_pesoCustoRefugo   * sum((pt,j,t)$pt_on(pt), (p_refugoLargura(pt)/s_larguraMae) * v_usaPadrao(pt,j,t));

eq_calcula_backlog.. v_somaBacklog =e= sum((p_base,w,c,t), Bcum(p_base,w,c,t));

eq_obj_lex.. zTotal =e= W_backlog * v_somaBacklog + v_custoProducao;

* --- Modelo Mestre ---
Model CorteMestre /
    eq_obj_lex, eq_calcula_custo, eq_calcula_backlog,
    eq_backlogCum, eq_capacidade
/;

* ------------------------------------------------------------------------------
* --- Subproblema ---
* ------------------------------------------------------------------------------

Set c_on(c) 'comprimentos ativos no conjunto p';
c_on(c) = yes$( sum((p_base,w)$p(p_base,w,c), 1) > 0 );

Parameter UmaxCuts(p_base,w,c) 'limite sup. inteiro de cortes por produto no padrao';
UmaxCuts(p_base,w,c) = 0;
UmaxCuts(p_base,w,c)$(
    p(p_base,w,c) and p_larguraPwC(p_base,w,c) > 0 and
    p_larguraPwC(p_base,w,c) <= s_larguraMae
) = floor(s_larguraMae / p_larguraPwC(p_base,w,c));

Scalar s_minUtilPct 'Target de utilizacao minima de largura por padrao' / 0.97 /;

Variables v_valorPadrao;
Integer Variable v_geraCorte(p_base,w,c);
Binary Variable  y_len(c) '1 se o padrao escolhe o comprimento c';

Equations
    eq_sub_objetivo, eq_sub_capacidade, eq_one_len,
    eq_link_len(p_base,w,c), eq_min_util;

eq_sub_objetivo..   v_valorPadrao =e= sum((p_base,w,c), p_precoDual(p_base,w,c) * v_geraCorte(p_base,w,c));
eq_sub_capacidade.. sum((p_base,w,c), p_larguraPwC(p_base,w,c) * v_geraCorte(p_base,w,c)) =l= s_larguraMae;
eq_one_len..        sum(c$c_on(c), y_len(c)) =e= 1;

eq_link_len(p_base,w,c)$p(p_base,w,c)..
    v_geraCorte(p_base,w,c) =l= UmaxCuts(p_base,w,c) * y_len(c);

eq_min_util..
    sum((p_base,w,c)$p(p_base,w,c), p_larguraPwC(p_base,w,c) * v_geraCorte(p_base,w,c))
    =g= s_minUtilPct * s_larguraMae * sum(c$c_on(c), y_len(c));

Model GeraPadrao /
    eq_sub_objetivo, eq_sub_capacidade, eq_one_len,
    eq_link_len, eq_min_util
/;
v_geraCorte.lo(p_base,w,c) = 0;
v_geraCorte.up(p_base,w,c) = UmaxCuts(p_base,w,c);

* ==============================================================================
* FASE 3: LOOP DE GERACAO DE COLUNAS
* ==============================================================================
Scalar
    s_contadorPadroes   / 0 /,   s_podeMelhorar  / 1 /,     s_iter / 0 /,
    s_iterMax           / 499 /, s_semMelhora / 0 /,     s_semMelhoraMax / 10 /,
    s_epsPreco          / 0.001 /, s_lastCusto / INF /,
    s_tempoMaximoTotal  / 300 /, s_timeStart;
Scalar p_fillLargura_cand 'Ocupacao do padrao candidato para validacao';

* --- NOVO: Inicializacao de um padrao "dummy" para a primeira iteracao ---
* O padrao dummy permite que o primeiro SOLVE execute sem erro ($66).
* Ele nao tem custo nem rendimento, sendo ignorado pela otimizacao.
p_rendimentoPadrao('pt_dummy', p_base, w, c) = 0;
p_refugoLargura('pt_dummy') = 0;
pt_on('pt_dummy') = yes;

* --- Loop Principal ---
s_timeStart = jnow;
WHILE( (s_podeMelhorar = 1) and (s_iter < s_iterMax) and (s_semMelhora < s_semMelhoraMax) and ((jnow - s_timeStart)*86400 < s_tempoMaximoTotal),
    s_iter = s_iter + 1;

    SOLVE CorteMestre using RMIP minimizing zTotal;

    p_precoDual(p_base,w,c) = sum(t, eq_backlogCum.m(p_base,w,c,t));
    put_utility 'log' / '[INFO] Iteracao ', s_iter:0:0, ' | Backlog: ', v_somaBacklog.l:0:4, ' | Custo: ', v_custoProducao.l:0:2;

    if( s_lastCusto < INF,
        if( abs(s_lastCusto - zTotal.l) < 1e-4, s_semMelhora = s_semMelhora + 1; else s_semMelhora = 0; );
    );
    s_lastCusto = zTotal.l;

    s_podeMelhorar = 0;
    SOLVE GeraPadrao using MIP maximizing v_valorPadrao;

    if (v_valorPadrao.l > 1 + s_epsPreco,
        put_utility 'log' / '[INFO] Novo padrao com valor: ', v_valorPadrao.l:0:4;
        s_podeMelhorar = 1;
        s_contadorPadroes = s_contadorPadroes + 1;

* NOVO: Desativa o padrao dummy apos a criacao do primeiro padrao real
        if(s_contadorPadroes = 1,
            pt_on('pt_dummy') = no;
        );

        loop(pt_new$(ord(pt_new) = s_contadorPadroes + 1),
* Logica de aceitacao com gatekeeper de qualidade para TODOS os padroes
           p_rendimentoPadrao(pt_new, p_base,w,c) = v_geraCorte.l(p_base,w,c);
           p_fillLargura_cand = sum((p_base,w,c), p_larguraPwC(p_base,w,c) * p_rendimentoPadrao(pt_new,p_base,w,c));

           if (p_fillLargura_cand >= s_minUtilPct_master * s_larguraMae,
               pt_on(pt_new) = yes;
               p_refugoLargura(pt_new) = s_larguraMae - p_fillLargura_cand;
               put_utility 'log' / '[ACCEPT] Padrao ', pt_new.tl, ' aceito com utilizacao: ', ((p_fillLargura_cand/s_larguraMae)*100):0:2, '%';
           else
               pt_on(pt_new) = no;
               p_rendimentoPadrao(pt_new, p_base,w,c) = 0;
               s_contadorPadroes = s_contadorPadroes - 1;
               put_utility 'log' / '[REJECT] Padrao ', pt_new.tl, ' rejeitado com utilizacao: ', ((p_fillLargura_cand/s_larguraMae)*100):0:2, '%';
           );
        );
    );
);

* ==============================================================================
* FASE 4: OTIMIZACAO FINAL E RELATORIOS
* ==============================================================================
if (CorteMestre.modelstat = 1 or CorteMestre.modelstat = 8,
    SOLVE CorteMestre using MIP minimizing zTotal;
);

* ------------------------------------------------------------------------------
* 4.1 Relatorios de Saida em formato CSV
* ------------------------------------------------------------------------------
File
    f_composicao_csv    '/relatorio_composicao_padroes.csv/'
    f_plano_csv         '/relatorio_plano_producao.csv/'
    f_status_csv        '/relatorio_status_entregas.csv/';

* --- Relatorio 1: Composicao dos Padroes Utilizados
put f_composicao_csv 'PadraoCorte,PN_Base,LarguraProduto,CompProduto,QtdPorBobinaMae' /;
loop((pt, p_base, w, c)$(p_rendimentoPadrao(pt, p_base, w, c) > 0),
    put f_composicao_csv pt.tl, ',', p_base.tl, ',', w.tl, ',', c.tl, ',';
    put f_composicao_csv p_rendimentoPadrao(pt,p_base,w,c):0:2 /;
);
putclose f_composicao_csv;

* --- Relatorio 2: Plano de Producao (com saida inteira)
put f_plano_csv 'DataProducao,Maquina,PadraoCorte,QtdBobinasMae' /;
loop((pt,j,t)$(v_usaPadrao.l(pt,j,t) > 0.001),
    put f_plano_csv t.tl, ',', j.tl, ',', pt.tl, ',';
    put f_plano_csv v_usaPadrao.l(pt,j,t):0:0 /;
);
putclose f_plano_csv;

* --- Relatorio 3: Status das Entregas vs. Demanda
Parameter
    p_prodPorData(p_base,w,c,t)
    p_cumProd(p_base,w,c,t)
    p_cumDem(p_base,w,c,t)
    p_dataAtende_ord(p_base,w,c,t)
    p_diasDesvio(p_base,w,c,t);

p_prodPorData(p_base,w,c,t) = sum((pt,j), p_rendimentoPadrao(pt,p_base,w,c) * v_usaPadrao.l(pt,j,t));
p_cumProd(p_base,w,c,t) = sum(tt$(ord(tt) <= ord(t)), p_prodPorData(p_base,w,c,tt));
p_cumDem(p_base,w,c,t)  = sum(tt$(ord(tt) <= ord(t)), p_demanda(p_base,w,c,tt));

p_dataAtende_ord(p_base,w,c,t) = 0;
loop((p_base,w,c,t)$(p_demanda(p_base,w,c,t) > 0),
    loop(t2$(p_cumProd(p_base,w,c,t2) >= p_cumDem(p_base,w,c,t) and p_dataAtende_ord(p_base,w,c,t) = 0),
        p_dataAtende_ord(p_base,w,c,t) = ord(t2);
    );
);
p_diasDesvio(p_base,w,c,t)$(p_dataAtende_ord(p_base,w,c,t) > 0) = p_dataAtende_ord(p_base,w,c,t) - ord(t);

put f_status_csv 'PN_Base,LarguraProduto,CompProduto,DataEntregaRequerida,QtdDemandada,DataProducaoReal,DiasDesvio,StatusEntrega' /;
loop((p_base,w,c,t)$(p_demanda(p_base,w,c,t) > 0),
    put f_status_csv p_base.tl, ',', w.tl, ',', c.tl, ',', t.tl, ',';
    put f_status_csv p_demanda(p_base,w,c,t):0:2, ',';

    if(p_dataAtende_ord(p_base,w,c,t) > 0,
        loop(t_prod$(ord(t_prod) = p_dataAtende_ord(p_base,w,c,t)),
            put f_status_csv t_prod.tl;
        );
        put f_status_csv ',', p_diasDesvio(p_base,w,c,t):0:0, ',';
        if (p_diasDesvio(p_base,w,c,t) > 0, put '1'; else put '2';);
    else
        put f_status_csv '1000-01-01 ,0 ,-1 ';
    );
    put /;
);
putclose f_status_csv;