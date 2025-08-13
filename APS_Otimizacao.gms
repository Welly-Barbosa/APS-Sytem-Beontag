* ==============================================================================
* GAMS Model: Otimizacao de Corte de Bobinas - Beontag
* Versao: 20.1 - GEEK (Refinamento Completo)
* Autor: GAMS Geek (Gemini AI)
* Data: 11 de Agosto de 2025
*
* DESCRICAO:
* Versao final baseada na v20.0, implementando todas as 4 correcoes (A,B,C,D).
* A: Criterio de aceitacao de coluna corrigido e sem arredondamentos.
* B: Filtro dual inteligente para acelerar o subproblema.
* C: Subproblema flexibilizado, penalizando a sobra em vez de usar uma
* restricao rigida de utilizacao.
* D: Adicionada trava e custo para o excesso de producao por SKU.
* ==============================================================================

$TITLE Otimizacao de Corte de Bobinas - Beontag (v20.1 - Refinamento Completo)

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
    pt       'Universo de padroes de corte potenciais' /pt001*pt999/;

Alias (t, tt, t2, t_prod);
Alias (pt, pt_new);

* --- Parametros para calculo da capacidade ---
Parameter p_velocidadeEfetiva(j), p_tempoProcesso(j), p_tempoUsoMaquina(j);
p_velocidadeEfetiva(j) = p_velocidadeMaq(j) * p_eficiencia(j);
p_tempoProcesso(j)$p_velocidadeEfetiva(j) = s_comprimentoMae_pes / p_velocidadeEfetiva(j);
p_tempoUsoMaquina(j) = p_tempoSetupBase(j) + p_tempoProcesso(j);

* --- Parametros de Custo e Qualidade ---
Scalar s_pesoCustoProducao   / 1 /;
Scalar W_backlog             /1e6/;
Scalar s_pesoCustoRefugo     / 10 /;
Scalar s_allowOveragePct     / 0.05 /;
Scalar s_pesoCustoExcedente  / 50 /;

Parameters
    p_precoDual(p_base,w,c)
    p_refugoLargura(pt);

* Mapeamento de dados dos produtos
Parameters p_larguraPwC(p_base,w,c), p_compPwC(p_base,w,c);
p_larguraPwC(p_base,w,c)$(p_larguraProduto(p_base,w,c)) = p_larguraProduto(p_base,w,c);
p_compPwC(p_base,w,c)$(p_comprimentoProduto(p_base,w,c)) = p_comprimentoProduto(p_base,w,c);

* ==============================================================================
* FASE 2: DEFINICAO DO PROBLEMA MESTRE E SUBPROBLEMA
* ==============================================================================

Parameters
    p_rendimentoPadrao(pt,p_base,w,c);
Set pt_on(pt);

* --- Variaveis e Equacoes do Mestre ---
Variables
    v_usaPadrao(pt, j, t)
    Bcum(p_base,w,c,t)
    v_custoProducao
    v_somaBacklog
    zTotal
    v_excedente(p_base,w,c)      'NOVO (D): Variavel de excesso de producao';

Integer Variable v_usaPadrao;
Positive Variable Bcum, v_excedente;

Equations
    eq_capacidade(j, t)
    eq_backlogCum(p_base,w,c,t)
    eq_calcula_custo
    eq_calcula_backlog
    eq_obj_lex
    eq_cap_overage(p_base,w,c) 'NOVO (D): Equacao de contencao de excesso';

eq_capacidade(j,t)$(p_tempoDisponivel(j,t) > 0)..
    sum(pt$pt_on(pt), v_usaPadrao(pt,j,t) * p_tempoUsoMaquina(j))
    =l= p_tempoDisponivel(j,t);

eq_backlogCum(p_base,w,c,t)..
    Bcum(p_base,w,c,t) =g=
        sum(t2$(ord(t2) <= ord(t)), p_demanda(p_base,w,c,t2))
      - sum((pt,j,t2)$(ord(t2) <= ord(t)), p_rendimentoPadrao(pt,p_base,w,c) * v_usaPadrao(pt,j,t2));

* REVISADO (D): O custo de producao agora inclui uma penalidade pelo excesso.
eq_calcula_custo..
    v_custoProducao =e=
        s_pesoCustoProducao * sum((pt,j,t)$pt_on(pt), v_usaPadrao(pt,j,t))
      + s_pesoCustoRefugo   * sum((pt,j,t)$pt_on(pt), (p_refugoLargura(pt)/s_larguraMae) * v_usaPadrao(pt,j,t))
      + s_pesoCustoExcedente* sum((p_base,w,c), v_excedente(p_base,w,c));

eq_calcula_backlog.. v_somaBacklog =e= sum((p_base,w,c,t), Bcum(p_base,w,c,t));

eq_obj_lex.. zTotal =e= W_backlog * v_somaBacklog + v_custoProducao;

* NOVO (D): Trava de superproducao por SKU.
eq_cap_overage(p_base,w,c)..
    sum((pt,j,t), p_rendimentoPadrao(pt,p_base,w,c) * v_usaPadrao(pt,j,t))
  =l= (1 + s_allowOveragePct) * sum(t, p_demanda(p_base,w,c,t))
    + v_excedente(p_base,w,c);


* --- Modelo Mestre ---
Model CorteMestre /
    eq_obj_lex, eq_calcula_custo, eq_calcula_backlog,
    eq_backlogCum, eq_capacidade, eq_cap_overage
/;

* ------------------------------------------------------------------------------
* --- Subproblema (Refatorado para ser flexivel) ---
* ------------------------------------------------------------------------------
Set c_on(c); Parameter UmaxCuts(p_base,w,c); Scalar s_penWaste / 0.05 /;
c_on(c) = yes$( sum((p_base,w)$p(p_base,w,c), 1) > 0 );
UmaxCuts(p_base,w,c)$(p(p_base,w,c) and p_larguraPwC(p_base,w,c) > 0) =
    floor( s_larguraMae / p_larguraPwC(p_base,w,c) );

Variables v_valorPadrao;
Integer Variable v_geraCorte(p_base,w,c);
Binary Variable  y_len(c);
Positive Variable wWaste 'NOVO (C): Variavel de sobra de largura';

* 'REVISADO (C)'
Equations
    eq_sub_objetivo, eq_sub_capacidade, eq_one_len,
    eq_link_len(p_base,w,c), eq_width_balance; 

* REVISADO (C): Objetivo com penalidade de sobra
eq_sub_objetivo..
    v_valorPadrao =e=
      sum((p_base,w,c), p_precoDual(p_base,w,c) * v_geraCorte(p_base,w,c))
    - s_penWaste * wWaste;

eq_sub_capacidade.. sum((p_base,w,c), p_larguraPwC(p_base,w,c) * v_geraCorte(p_base,w,c)) =l= s_larguraMae;
eq_one_len..        sum(c$c_on(c), y_len(c)) =e= 1;
eq_link_len(p_base,w,c)$p(p_base,w,c)..
    v_geraCorte(p_base,w,c) =l= UmaxCuts(p_base,w,c) * y_len(c);

* NOVO (C): Balanco de largura que define a sobra (wWaste)
eq_width_balance..
    sum((p_base,w,c), p_larguraPwC(p_base,w,c) * v_geraCorte(p_base,w,c)) + wWaste
    =e= s_larguraMae * sum(c$c_on(c), y_len(c));

Model GeraPadrao / eq_sub_objetivo, eq_sub_capacidade, eq_one_len, eq_link_len, eq_width_balance /;
v_geraCorte.lo(p_base,w,c) = 0;
v_geraCorte.up(p_base,w,c) = UmaxCuts(p_base,w,c);

* ==============================================================================
* FASE 3: LOOP DE GERACAO DE COLUNAS
* ==============================================================================
Scalar
    s_contadorPadroes   / 0 /, s_podeMelhorar  / 1 /,     s_iter / 0 /,
    s_iterMax           / 499 /, s_semMelhora / 0 /,     s_semMelhoraMax / 10 /,
    s_epsRedCost        / 1e-6 /, s_lastCusto / INF /,
    s_tempoMaximoTotal  / 300 /, s_timeStart,
    epsDual             / 1e-9 /;
Set lenActive(c);

* --- Geracao do Padrao Inicial (Seed) ---
Parameter p_temDemanda(p_base,w,c), p_order_id(p_base,w,c);
Scalar s_min_id_com_demanda;
p_temDemanda(p_base,w,c)$(sum(t, p_demanda(p_base,w,c,t)) > 0) = 1;
p_order_id(p_base,w,c) = ord(p_base) * (card(w)*card(c)) + ord(w) * card(c) + ord(c);
s_min_id_com_demanda = smin((p_base,w,c)$p_temDemanda(p_base,w,c), p_order_id(p_base,w,c));
s_contadorPadroes = 1;
loop(pt$(ord(pt) = s_contadorPadroes),
    pt_on(pt) = yes;
    p_rendimentoPadrao(pt, p_base,w,c)$(p_order_id(p_base,w,c) = s_min_id_com_demanda) = 1;
    p_refugoLargura(pt) = s_larguraMae - sum((p_base,w,c),
        p_larguraPwC(p_base,w,c) * p_rendimentoPadrao(pt, p_base,w,c));
);

* --- Loop Principal ---
s_timeStart = jnow;
WHILE( (s_podeMelhorar = 1) and (s_iter < s_iterMax) and ((jnow - s_timeStart)*86400 < s_tempoMaximoTotal),
    s_iter = s_iter + 1;

    SOLVE CorteMestre using RMIP minimizing zTotal;

    p_precoDual(p_base,w,c) = sum(t, eq_backlogCum.m(p_base,w,c,t));
    put_utility 'log' / '[INFO] Iteracao ', s_iter:0:0, ' | Backlog: ', v_somaBacklog.l:0:4, ' | Custo: ', v_custoProducao.l:0:2;

    if( s_lastCusto < INF,
        if( abs(s_lastCusto - zTotal.l) < 1e-4, s_semMelhora = s_semMelhora + 1; else s_semMelhora = 0; );
    );
    s_lastCusto = zTotal.l;
    s_podeMelhorar = 0;

* --- NOVO (B): Bloco de Filtro Dual Inteligente ---
    v_geraCorte.up(p_base,w,c) = UmaxCuts(p_base,w,c);
    y_len.up(c) = 1;
    v_geraCorte.up(p_base,w,c)$(p_precoDual(p_base,w,c) <= epsDual) = 0;
    lenActive(c) = no;
    lenActive(c) = yes$( sum((p_base,w), v_geraCorte.up(p_base,w,c)) );
    y_len.up(c)$(not lenActive(c)) = 0;
    if (sum(c, y_len.up(c)) = 0,
        v_geraCorte.up(p_base,w,c)$(p_temDemanda(p_base,w,c) and UmaxCuts(p_base,w,c)>=1)
            = UmaxCuts(p_base,w,c);
        y_len.up(c) = 1;
        put_utility 'log' / '[WARN] Filtro dual muito restritivo, fallback ativado.';
    );

    SOLVE GeraPadrao using MIP maximizing v_valorPadrao;

* REVISADO (A): Criterio de aceitacao corrigido
    if (v_valorPadrao.l > s_epsRedCost,
        put_utility 'log' / '[INFO] Novo padrao com valor: ', v_valorPadrao.l:0:4;
        s_podeMelhorar = 1;
        s_contadorPadroes = s_contadorPadroes + 1;
        loop(pt_new$(ord(pt_new) = s_contadorPadroes),
           pt_on(pt_new) = yes;
* REVISADO (A): Atribuicao direta sem arredondamento
           p_rendimentoPadrao(pt_new, p_base,w,c) = v_geraCorte.l(p_base,w,c);
           p_refugoLargura(pt_new) = s_larguraMae - sum((p_base,w,c),
               p_larguraPwC(p_base,w,c) * p_rendimentoPadrao(pt_new, p_base,w,c));
        );
    );
);

* ==============================================================================
* FASE 4: OTIMIZACAO FINAL E RELATORIOS
* ==============================================================================
CorteMestre.optcr= 0.01;
CorteMestre.reslim = 300;
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
Scalar s_code;
loop((p_base,w,c,t)$(p_demanda(p_base,w,c,t) > 0),
    put f_status_csv p_base.tl, ',', w.tl, ',', c.tl, ',', t.tl, ',';
    put f_status_csv p_demanda(p_base,w,c,t):0:2, ',';

    if (p_dataAtende_ord(p_base,w,c,t) > 0,
        loop(t_prod$(ord(t_prod) = p_dataAtende_ord(p_base,w,c,t)),
            put f_status_csv t_prod.tl;
        );
        put f_status_csv ',';
        put f_status_csv p_diasDesvio(p_base,w,c,t):0:0, ',';
        s_code = 0$(p_diasDesvio(p_base,w,c,t) <= 0) + 1$(p_diasDesvio(p_base,w,c,t) > 0);
        put f_status_csv s_code:0:0 /;
    else
        put f_status_csv ' , ,-1' /;
    );
);
putclose f_status_csv;