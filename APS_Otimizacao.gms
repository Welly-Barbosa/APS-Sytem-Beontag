* ==============================================================================
* GAMS Model: Otimizacao de Corte de Bobinas - Beontag
* Versao: 28.0 - GEEK (Filtro de Par Valido)
* Autor: GAMS Geek (Gemini AI)
* Data: 20 de Agosto de 2025
*
* DESCRICAO:
* Esta versao corrige a falha de geracao de padroes da v27.0 implementando
* um filtro unificado e robusto para o subproblema.
* 1. Um novo Set 'p_pc_validos(p_base, c)' e criado dinamicamente para
* armazenar apenas as combinacoes (PN_Base, Comprimento) que existem.
* 2. O subproblema foi reestruturado para usar uma unica variavel binaria
* 'y_par_pc' que seleciona um desses pares validos.
* 3. Esta abordagem garante que o subproblema sempre opere com um conjunto
* viavel de produtos, resolvendo a falha e tornando o modelo mais eficiente.
* ==============================================================================

$TITLE Otimizacao de Corte de Bobinas - Beontag (v28.0 - Filtro de Par)

* ------------------------------------------------------------------------------
* FASE 0: OPCOES GLOBAIS
* ------------------------------------------------------------------------------
$onDotL

* --- Opcoes do Solver
*Option optcr  = 0.01;
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
    pt       'Universo de padroes de corte potenciais' /pt_dummy, Job001*Job999/
*-- NOVO --*
    p_pc_validos(p_base, c) 'Pares (PN_Base, Comprimento) que existem nos dados';

Alias (t, tt, t2, t_prod);
Alias (pt, pt_new);

* --- Populamos dinamicamente o novo Set com as combinacoes que realmente existem
p_pc_validos(p_base, c) = yes$sum(w, p(p_base, w, c));


* --- Parametros para calculo da capacidade ---
Parameter p_velocidadeEfetiva(j) 'pol/min efetivo';
p_velocidadeEfetiva(j) = p_velocidadeMaq(j) * p_eficiencia(j);

Parameter p_tempoProcesso(j) 'min por bobina-mae';
p_tempoProcesso(j)$p_velocidadeEfetiva(j) = s_comprimentoMae_pes / p_velocidadeEfetiva(j);
Parameter p_tempoUsoMaquina(j) 'min por uso na maquina j (setup + processo)';
p_tempoUsoMaquina(j) = p_tempoSetupBase(j) + p_tempoProcesso(j);

* --- Parametros de Custo e Qualidade ---
Scalar s_pesoCustoProducao         'Custo por bobina-mae usada' / 1 /;
Scalar s_pesoCustoAtraso          'Penalidade ponderada por dia de atraso'/ 10 /;
Scalar s_pesoCustoFalta        'Custo por unidade de produto faltante (demanda nao atendida)'/ 500000 /;
Scalar s_pesoCustoRefugo         'Custo de refugo (sobra de largura), para desempate'/ 0.1 /;

Scalar s_minUtilPct_master    'Utilizacao minima para um padrao ser aceito pelo mestre' / 0.70 /;

* --- Parametro para a restricao global de cortes ---
Scalar s_maxCortesGlobal     'Numero maximo de cortes permitido em um padrao';
s_maxCortesGlobal = smin(j, p_maxCortes(j));


Parameters
    p_precoDual(p_base,w,c)         'Preco dual da demanda (para pricing)'
    p_refugoLargura(pt)             'Largura nao utilizada (refugo) do padrao pt'
    p_custoAtraso(p_base, w, c, t)  'Penalidade por dia de atraso para cada necessidade';

* Mapeamento de dados dos produtos
Parameters
    p_larguraPwC(p_base,w,c)    'Largura numerica do produto'
    p_compPwC(p_base,w,c)       'Comprimento numerico do produto';
p_larguraPwC(p_base,w,c)$(p_larguraProduto(p_base,w,c)) = p_larguraProduto(p_base,w,c);
p_compPwC(p_base,w,c)$(p_comprimentoProduto(p_base,w,c)) = p_comprimentoProduto(p_base,w,c);

* --- Calculo da penalidade de atraso ---
p_custoAtraso(p_base,w,c,t)$p_demanda(p_base,w,c,t) = (1 / (ord(t) + 1e-6)) * p_demanda(p_base,w,c,t);


* ==============================================================================
* FASE 2: DEFINICAO DO PROBLEMA MESTRE E SUBPROBLEMA
* ==============================================================================

Parameters
    p_rendimentoPadrao(pt,p_base,w,c) 'Qtd do produto (p,w,c) por padrao pt';
Set pt_on(pt) 'Padroes ativos que podem ser usados pelo mestre';

* --- Variaveis e Equacoes do Mestre ---
Variables
    v_usaPadrao(pt, j, t)
    v_diasAtraso(p_base, w, c, t)
    v_falta(p_base, w, c)
    zTotal;
Integer Variable  v_usaPadrao;
Positive Variable v_diasAtraso, v_falta;

Equations
    eq_objetivo
    eq_atendeDemanda(p_base, w, c)
    eq_calculaAtraso(p_base, w, c, t)
    eq_capacidade(j, t);

*
* A funcao objetivo minimiza a soma ponderada de todos os custos.
eq_objetivo.. zTotal =e=
      s_pesoCustoProducao  * sum((pt,j,t)$pt_on(pt), v_usaPadrao(pt,j,t))
    + s_pesoCustoRefugo    * sum((pt,j,t)$pt_on(pt), (p_refugoLargura(pt)/s_larguraMae) * v_usaPadrao(pt,j,t))
    + s_pesoCustoAtraso    * sum((p_base,w,c,t), v_diasAtraso(p_base,w,c,t))
    + s_pesoCustoFalta     * sum((p_base,w,c), v_falta(p_base,w,c));

*
* Garante que a producao total de um item, somada a falta, seja EXATAMENTE igual a demanda total.
eq_atendeDemanda(p_base,w,c)..
    sum((pt,j,t)$pt_on(pt), p_rendimentoPadrao(pt, p_base,w,c) * v_usaPadrao(pt,j,t)) + v_falta(p_base,w,c)
    =e= sum(t, p_demanda(p_base,w,c, t));

*
* O atraso e calculado para cada necessidade especifica, ponderado pela quantidade produzida apos o prazo.
eq_calculaAtraso(p_base,w,c,t)$p_demanda(p_base,w,c,t)..
    v_diasAtraso(p_base,w,c,t) =g= sum((pt,j,t2)$(pt_on(pt) and ord(t2) > ord(t)),
        (ord(t2) - ord(t)) * p_custoAtraso(p_base,w,c,t) * (p_rendimentoPadrao(pt,p_base,w,c) * v_usaPadrao(pt,j,t2))
    );

*
* Garante que o tempo total de uso dos padroes em uma maquina j no dia t nao exceda o tempo disponivel.
eq_capacidade(j,t)$(p_tempoDisponivel(j,t) > 0)..
    sum(pt$pt_on(pt), v_usaPadrao(pt,j,t) * p_tempoUsoMaquina(j))
    =l= p_tempoDisponivel(j,t);

* --- Modelo Mestre ---
Model CorteMestre / all /;

* ------------------------------------------------------------------------------
* --- Subproblema (Logica ALTERADA para Par Valido) ---
* ------------------------------------------------------------------------------
Parameter UmaxCuts(p_base,w,c) 'limite sup. inteiro de cortes por produto no padrao';
UmaxCuts(p_base,w,c) = 0;
UmaxCuts(p_base,w,c)$(
    p(p_base,w,c) and p_larguraPwC(p_base,w,c) > 0 and
    p_larguraPwC(p_base,w,c) <= s_larguraMae
) = floor(s_larguraMae / p_larguraPwC(p_base,w,c));
Scalar s_minUtilPct 'Target de utilizacao minima de largura por padrao' / 0.75 /;

Variables
    v_valorPadrao;
Integer Variable v_geraCorte(p_base,w,c);
*-- ALTERADO --*
Binary Variable
    y_par_pc(p_base, c) '1 se o par (PN, Comp) for escolhido para o padrao';

Equations
    eq_sub_objetivo,
    eq_sub_capacidade,
*-- NOVO --*
    eq_um_par_pc,
    eq_link_par_pc(p_base,w,c),
    eq_min_util,
    eq_sub_max_cortes;

*
* Funcao objetivo do subproblema: maximiza o valor do padrao.
eq_sub_objetivo..   v_valorPadrao =e= sum((p_base,w,c), p_precoDual(p_base,w,c) * v_geraCorte(p_base,w,c));
*
* Restricao de capacidade de largura.
eq_sub_capacidade.. sum((p_base,w,c), p_larguraPwC(p_base,w,c) * v_geraCorte(p_base,w,c)) =l= s_larguraMae;

*-- NOVO --*
*
* Garante que exatamente um par valido (PN_Base, Comprimento) seja escolhido.
eq_um_par_pc..
    sum(p_pc_validos(p_base,c), y_par_pc(p_base,c)) =e= 1;

*-- NOVO --*
*
* Acopla a variavel de corte 'v_geraCorte' a escolha do par (PN_Base, Comprimento).
eq_link_par_pc(p_base,w,c)$p(p_base,w,c)..
    v_geraCorte(p_base,w,c) =l= UmaxCuts(p_base,w,c) * y_par_pc(p_base,c);

*
* Garante que a utilizacao da largura da bobina-mae seja acima de um percentual minimo.
*-- ALTERADO (simplificado, pois a soma das variaveis binarias agora e sempre 1)--*
eq_min_util..
    sum((p_base,w,c)$p(p_base,w,c), p_larguraPwC(p_base,w,c) * v_geraCorte(p_base,w,c))
    =g= s_minUtilPct * s_larguraMae;
*
* Restricao de numero maximo de cortes.
eq_sub_max_cortes..
    sum((p_base,w,c), v_geraCorte(p_base,w,c)) =l= s_maxCortesGlobal;

Model GeraPadrao / all /;

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

* --- Inicializacao com padrao "dummy" ---
p_rendimentoPadrao('pt_dummy', p_base, w, c) = 0;
p_refugoLargura('pt_dummy') = 0;
pt_on('pt_dummy') = yes;

* --- Loop Principal ---
s_timeStart = jnow;
WHILE( (s_podeMelhorar = 1) and (s_iter < s_iterMax) and (s_semMelhora < s_semMelhoraMax) and ((jnow - s_timeStart)*86400 < s_tempoMaximoTotal),
    s_iter = s_iter + 1;

    SOLVE CorteMestre using RMIP minimizing zTotal;

    p_precoDual(p_base,w,c) = eq_atendeDemanda.m(p_base,w,c);
    put_utility 'log' / '[INFO] Iteracao ', s_iter:0:0, ' | Custo Mestre: ', zTotal.l:0:2;

    if( s_lastCusto < INF,
        if( abs(s_lastCusto - zTotal.l) < 1e-4, s_semMelhora = s_semMelhora + 1; else s_semMelhora = 0; );
    );
    s_lastCusto = zTotal.l;

    s_podeMelhorar = 0;
    SOLVE GeraPadrao using MIP maximizing v_valorPadrao;

    if (v_valorPadrao.l > s_pesoCustoProducao + s_epsPreco,
        put_utility 'log' / '[INFO] Novo padrao com valor: ', v_valorPadrao.l:0:4;
        s_podeMelhorar = 1;
        s_contadorPadroes = s_contadorPadroes + 1;

        if(s_contadorPadroes = 1,
            pt_on('pt_dummy') = no;
        );

        loop(pt_new$(ord(pt_new) = s_contadorPadroes + 1),
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
    CorteMestre.reslim=300;
    CorteMestre.optcr=0.05;
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