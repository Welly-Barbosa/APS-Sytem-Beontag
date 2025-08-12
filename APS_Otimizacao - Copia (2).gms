* ==============================================================================
* GAMS Model: Otimizacao de Corte de Bobinas - Beontag
* Versao: 15.1 - GEEK (Loop Dinamico e Correcoes Finais)
* Autor: GAMS Geek (Gemini AI)
* Data: 01 de Agosto de 2025
*
* DESCRICAO:
* Versao robusta que cria dinamicamente o conjunto de comprimentos a serem
* otimizados com base nos dados de entrada, eliminando a necessidade de
* um Set 'c' estatico e redundante.
* ==============================================================================

$TITLE Otimizacao de Corte de Bobinas - Beontag (v15.1 - GEEK)

* ------------------------------------------------------------------------------
* FASE 0: OPCOES GLOBAIS
* ------------------------------------------------------------------------------
$onDotL

* --- Opcoes do Solver
*Option optcr  = 0.15;
*Option reslim = 3600;
Option solPrint = off;
Option limRow = 0, limCol = 0;

* --- Opcoes especificas para o CPLEX
$onecho > cplex.opt
Option  mipemphasis= 1;
Option threads= 0;
Option  heurfreq= 5;
$offecho

* ==============================================================================
* FASE 1: CARGA DE DADOS E DEFINICAO DE ESTRUTURAS
* ==============================================================================

* ------------------------------------------------------------------------------
* 1.1 Inclusao do arquivo de dados
* ------------------------------------------------------------------------------
* O modelo espera que TODOS os conjuntos e parametros de entrada
* sejam fornecidos por este unico arquivo .dat.
$INCLUDE 'GamsInputData.dat'

* ------------------------------------------------------------------------------
* 1.2 Declaracao de Conjuntos Dinamicos e Auxiliares
* ------------------------------------------------------------------------------
* Declaracoes complementares (nao presentes no .dat)
Sets
    pt       'Padroes de corte'
    ;

Alias (t,tt,t2,t_prod);

* Parametros adicionais do modelo (nao presentes no .dat)
Parameters
    p_precoDual(p_base,w,c)     'preco dual da demanda (para pricing)'
  , p_custoAtraso(p,t)          'custo por dia de atraso (default=1)'
    ;

p_custoAtraso(p,t) = 1;

* Mapas (p -> p_base,w,c) para usar valores numericos por tupla
Parameters
    p_larguraPwC(p_base,w,c)    'largura produto (mm) mapeada de p'
    p_compPwC(p_base,w,c)       'comprimento produto (m) mapeado de p';

p_larguraPwC(p_base,w,c) = sum(p$ p(p_base,w,c), p_larguraProduto(p));
p_compPwC(p_base,w,c)    = sum(p$ p(p_base,w,c), p_comprimentoProduto(p));

* Validacao de largura (produtos maiores que a bobina-mae)

* Validacao de largura (produtos maiores que a bobina-mae)
Set p_erro_largura_mae(p_base, w, c);
p_erro_largura_mae(p_base, w, c)$(p(p_base, w, c) and p_larguraPwC(p_base, w, c) > s_larguraMae) = yes;

if(card(p_erro_largura_mae) > 0,
    display "ERRO - Produto com largura maior que a bobina-mae:", p_erro_largura_mae;
);

* ------------------------------------------------------------------------------
* 2. CONSTRUCAO DE PADROES (definicoes gerais)
* ------------------------------------------------------------------------------

Parameters
    p_rendimentoPadrao(pt,p_base,w,c) 'Qtd do produto (p_base,w,c) por padrao pt'
    p_numCortes(pt)          'Numero de cortes por padrao'
    ;

p_rendimentoPadrao(pt,p_base,w,c) = 0;
p_numCortes(pt) = 0;

* Padrões ativos (gerados no CG)
Set pt_on(pt) 'padroes ativos (ja gerados no CG)';
pt_on(pt) = no;

* ------------------------------------------------------------------------------
* 3. MESTRE: Variaveis e Equacoes
* ------------------------------------------------------------------------------

Variables
    v_usaPadrao(pt, j, t)
    v_excedente(p)
    v_diasAtraso(p, t)
    v_falta(p)
    z_custoTotal;

Binary Variable v_usaPadrao;
Positive Variables v_excedente, v_falta;

Equations
    eq_objetivo
    eq_atendeDemanda(p)
    eq_calculaAtraso(p,t)
    eq_capacidade(j, t)
    eq_preencheCapacidade(j);
  
* A funcao objetivo minimiza a soma ponderada de todos os custos.
eq_objetivo.. z_custoTotal =e=
      s_pesoCustoProducao * sum((pt,j,t), v_usaPadrao(pt,j,t))
    + s_pesoCustoAtraso   * sum((p,t), p_custoAtraso(p,t) * v_diasAtraso(p,t))
    + s_pesoCustoExcedente* sum(p, v_excedente(p))
    + s_pesoCustoFalta    * sum(p, v_falta(p));
    
    
* A producao total de um produto deve atender a demanda total (soma de todos os dias).
eq_atendeDemanda(p)..
    sum((pt,j,t,(p_base,w,c))$(pt_on(pt) and p(p_base,w,c)), p_rendimentoPadrao(pt, p_base,w,c) * v_usaPadrao(pt,j,t)) + v_falta(p)
    =e= sum(t, p_demanda(p, t)) + v_excedente(p);

* O atraso e calculado para cada necessidade especifica 
* (exemplo ilustrativo: manter se fizer sentido no seu caso)
eq_calculaAtraso(p,t).. v_diasAtraso(p,t) =g= 0;

* Capacidade por maquina e dia
Parameter p_tempoDisponivel(j,t) 'tempo disponivel na maquina j no dia t (min)';
Parameter s_minTempoPorMaquina(j) 'tempo minimo acumulado por maquina (min)';

* velocidade efetiva = velocidade nominal * eficiencia
Parameter p_velocidadeEfetiva(j) 'm/min efetivo por maquina';
p_velocidadeEfetiva(j) = p_velocidadeMaq(j) * p_eficiencia(j);
s_minTempoPorMaquina(j) = 0;

eq_capacidade(j,t)..    
    sum(pt$pt_on(pt), v_usaPadrao(pt,j,t)) * ( p_tempoSetupBase(j) + s_comprimentoMae_pes / p_velocidadeEfetiva(j) )
    =l= p_tempoDisponivel(j,t);

eq_preencheCapacidade(j).. sum(t, p_tempoDisponivel(j,t)) =g= s_minTempoPorMaquina(j);

* --- Patch: Objetivo de Servico (Etapa 1) e Fixacao de Backlog (Etapa 2) ---
Scalar Z1min 'backlog minimo obtido na Etapa 1' /0/;

Variable zServico 'Soma total do backlog (objetivo da Etapa 1)';
Equation eq_z1 'definicao do objetivo de servico (Etapa 1)'
         , eq_fixBacklog 'fixa soma do backlog na Etapa 2';

eq_z1..
    zServico =e= sum(p, v_falta(p));

eq_fixBacklog..
    sum(p, v_falta(p)) =e= Z1min;
Model CorteMestre_RMIP /eq_objetivo, eq_atendeDemanda, eq_calculaAtraso, eq_capacidade, eq_preencheCapacidade/;
Model CorteMestre_Final /CorteMestre_RMIP/;

Model CorteMestre_Service /eq_z1, eq_atendeDemanda, eq_calculaAtraso, eq_capacidade, eq_preencheCapacidade/;
Model CorteMestre_Cost    /eq_objetivo, eq_atendeDemanda, eq_calculaAtraso, eq_capacidade, eq_preencheCapacidade, eq_fixBacklog/;


* ------------------------------------------------------------------------------
* 3.3 Subproblema (Gerador de Padroes)
* ------------------------------------------------------------------------------

Variables z_valorPadrao;
Integer Variable v_geraCorte(p_base,w,c);

Equations eq_sub_capacity, eq_sub_objetivo;

eq_sub_capacity..  sum((p_base,w,c)$p(p_base,w,c), p_larguraPwC(p_base,w,c) * v_geraCorte(p_base,w,c)) =l= s_larguraMae;

eq_sub_objetivo..  z_valorPadrao =e= sum((p_base,w,c)$p(p_base,w,c), p_precoDual(p_base,w,c) * v_geraCorte(p_base,w,c));

Model GeraPadrao /eq_sub_capacity, eq_sub_objetivo/;

* bounds inteiras por produto: maximo de cortes possiveis por bobina-mae
v_geraCorte.lo(p_base,w,c) = 0;
v_geraCorte.up(p_base,w,c) = floor( s_larguraMae / p_larguraPwC(p_base,w,c) );


* ------------------------------------------------------------------------------
* 4. FASE 4: Loop de Geracao de Colunas (com controles robustos)
* ------------------------------------------------------------------------------

Scalar
    s_contadorPadroes   'Contador de padroes gerados'
    s_podeMelhorar      'Flag de controle do loop: 1 = continua, 0 = para'
    s_tempoMaximoTotal  'Tempo maximo total de execucao em segundos' / 300 /
    s_iter              'Iteracao atual CG' / 0 /
    s_iterMax           'Limite maximo de iteracoes' / 500 /
    s_semMelhora        'Iteracoes consecutivas sem melhora' / 0 /
    s_semMelhoraMax     'Limite de iteracoes sem melhora' / 30 /
    s_epsPreco          'Tolerancia do valor do padrao (reduced cost)' / 0.005 /
    s_lastCusto         'Ultimo custo mestre observado' / INF /
    s_timeStart         'JNOW inicio'
    s_elapsed           'Segundos decorridos'
;

Parameters
    p_lastPadrao(p_base,w,c)     'Assinatura do ultimo padrao gerado'
    p_candidato(p_base,w,c)      'Candidato atual de cortes';
    
p_lastPadrao(p_base,w,c) = -1;
p_candidato(p_base,w,c)  =  0;

p_rendimentoPadrao(pt, p_base,w,c) = 0;

* Seed: garante pelo menos um registro definido antes do 1o SOLVE do Service
if(card(pt) = 0,
    pt('pt_seed') = yes;
);
pt_on('pt_seed') = no;
* cria registros (zeros) para todo p no padrao seed
p_rendimentoPadrao('pt_seed',p_base,w,c) = 0;

p_numCortes(pt) = 0;

* Tempo total por padrao e maquina (min)
Parameter p_tempoTotalPadrao(pt,j) 'tempo por padrao na maquina (min)';

s_contadorPadroes = 0;
s_timeStart = jnow;

* --- Preparacao segura de p_c (parametro 0/1) ---
* Se p_c ainda nao estiver preenchido nesta fase, derive a partir da demanda
* (evita erro 66 no subproblema: simbolo p_c sem valores)
*p_c como parametro 0/1:
Parameter p_hasDemand(p_base,w,c) 'indicador de demanda > 0';
p_hasDemand(p_base,w,c) = 0;
p_hasDemand(p_base,w,c)$( sum(t, sum(p$p(p_base,w,c), p_demanda(p,t))) > 0 ) = 1;

put_utility 'log' / '[INFO] Usando pesos de WARM-UP para a geracao de colunas.';
s_pesoCustoProducao= 1;
s_pesoCustoAtraso = 10;
s_pesoCustoExcedente= 0.1;

Option optcr = 0.02;

s_podeMelhorar = 1;
WHILE( (s_podeMelhorar = 1)
       and (s_iter < s_iterMax)
       and (s_semMelhora < s_semMelhoraMax)
       and ( (jnow - s_timeStart)*86400 < s_tempoMaximoTotal ),

    s_iter = s_iter + 1;

    p_tempoTotalPadrao(pt, j)$(s_contadorPadroes >= ord(pt) and p_velocidadeEfetiva(j) > 0) =
        p_tempoSetupBase(j) + (s_comprimentoMae_pes / p_velocidadeEfetiva(j));

    * Etapa 1: minimizar backlog
    CorteMestre_RMIP.reslim = 60;
    CorteMestre_RMIP.optcr = 0.02;
    CorteMestre_Service.reslim = 60;
    CorteMestre_Service.optcr = 0.02;
    $onImplicitAssign
    SOLVE CorteMestre_Service using RMIP minimizing zServico;
    Z1min = zServico.l;

    * Etapa 2: minimizar custo com backlog fixo
    CorteMestre_Cost.reslim = 60;
    CorteMestre_Cost.optcr  = 0.02;
    SOLVE CorteMestre_Cost using RMIP minimizing z_custoTotal;
    $offImplicitAssign

    p_precoDual(p_base,w,c) = sum(p$p(p_base,w,c), eq_atendeDemanda.m(p));

    put_utility 'log' / '[MONITORAMENTO] Iteracao ', s_iter:0:0, ' | Custo Mestre: ', z_custoTotal.l:0:6;

    if( s_lastCusto < INF,
        if( abs(s_lastCusto - z_custoTotal.l) < 1e-4,
            s_semMelhora = s_semMelhora + 1;
        else
            s_semMelhora = 0;
        );
    );
    s_lastCusto = z_custoTotal.l;

    s_podeMelhorar = 0;
    LOOP(j,
        * v_geraCorte.up(p) = 1;  * removido: agora upper bound = floor(s_larguraMae / largura_p) (ver declaracao apos GeraPadrao)
        
        GeraPadrao.reslim = 120;
        GeraPadrao.optcr  = 0.02;
        SOLVE GeraPadrao using MIP maximizing z_valorPadrao;
        
        if(z_valorPadrao.l > 1 + s_epsPreco,
            put_utility 'log' / '[MONITORAMENTO] j=', j.tl, ' | Valor do Padrao: ', z_valorPadrao.l:0:6;
        );

        p_candidato(p_base,w,c) = round(v_geraCorte.l(p_base,w,c));
        if( (z_valorPadrao.l > (1 + s_epsPreco)) and (sum((p_base,w,c)$p_hasDemand(p_base,w,c), abs(p_candidato(p_base,w,c) - p_lastPadrao(p_base,w,c))) > 0),
            s_podeMelhorar  = 1;
            s_contadorPadroes = s_contadorPadroes + 1;
            p_rendimentoPadrao(pt, p_base,w,c)$(ord(pt) = s_contadorPadroes and p_hasDemand(p_base,w,c)) = p_candidato(p_base,w,c);
            pt_on(pt)$(ord(pt) = s_contadorPadroes) = yes;
            p_numCortes(pt)$(ord(pt) = s_contadorPadroes) = sum((p_base,w,c)$p_hasDemand(p_base,w,c), p_rendimentoPadrao(pt, p_base,w,c));
            p_lastPadrao(p_base,w,c) = p_candidato(p_base,w,c);
        );
    );

    if( s_semMelhora >= 10 and s_epsPreco < 0.02,
        s_epsPreco = 0.02;
        put_utility 'log' / '[MONITORAMENTO] Aumentando tolerancia de preco para ', s_epsPreco:0:3;
    );
);


* ------------------------------------------------------------------------------
* 5. FASE 5.2: Relatorios CSV + Mapeamento Producao→Ordens
* ------------------------------------------------------------------------------

File
    f_composicao_csv    '/relatorio_composicao_padroes.csv/'
    f_plano_csv         '/relatorio_plano_producao.csv/'
    f_status_csv        '/relatorio_status_entregas.csv/';

* 5.2.1 Composicao dos padroes
put f_composicao_csv 'PadraoCorte,PN_Base,LarguraProduto,CompProduto,QtdPorBobinaMae' /;
loop((pt,p_base,w,c)$(p_rendimentoPadrao(pt,p_base,w,c) > 0),
    put f_composicao_csv pt.tl, ',', p_base.tl, ',';
    put f_composicao_csv p_larguraPwC(p_base,w,c):0:4, ',', p_compPwC(p_base,w,c):0:0, ',';
    put f_composicao_csv p_rendimentoPadrao(pt,p_base,w,c):0:2 /;
);
putclose f_composicao_csv;

* 5.2.2 Plano de producao
put f_plano_csv 'DataProducao,Maquina,PadraoCorte,QtdBobinasMae' /;
loop((pt,j,t)$(v_usaPadrao.l(pt,j,t) > 0.001),
    put f_plano_csv t.tl, ',', j.tl, ',', pt.tl, ',';
    put f_plano_csv v_usaPadrao.l(pt,j,t):0:2 /;
);
putclose f_plano_csv;

* 5.2.3 Mapeamento cumulativo producao->demanda por produto e data
Parameter
    p_prodPorData(p_base,w,c,t)   'quantidade produzida do produto na data t'
    p_cumProd(p_base,w,c,t)       'producao acumulada ate t'
    p_cumDem(p_base,w,c,t)        'demanda acumulada ate t'
    p_demandaPwct(p_base,w,c,t)   'demanda por (p_base,w,c,t) agregada a partir de p'
    p_dataAtende_ord(p_base,w,c,t) 'ord(t_producao) que atende a demanda em t'
    p_diasDesvio(p_base,w,c,t)     'dias de desvio (inteiro)'
    ;

* Demanda agregada por tuple (p_base,w,c,t)
p_demandaPwct(p_base,w,c,t) = sum(p$p(p_base,w,c), p_demanda(p,t));

* Producao por data a partir das decisoes (usa mapeamento p(p_base,w,c))
p_prodPorData(p_base,w,c,t) = sum((pt,j), p_rendimentoPadrao(pt,p_base,w,c) * v_usaPadrao.l(pt,j,t));

* Acumulados por data
p_cumProd(p_base,w,c,t) = sum(tt$(ord(tt) <= ord(t)), p_prodPorData(p_base,w,c,tt));
p_cumDem(p_base,w,c,t)  = sum(tt$(ord(tt) <= ord(t)), p_demandaPwct(p_base,w,c,tt));

* Menor data de producao que cobre a demanda acumulada
p_dataAtende_ord(p_base,w,c,t) = 0;
loop((p_base,w,c,t)$ (p_demandaPwct(p_base,w,c,t) > 0),
    loop(t2$(p_cumProd(p_base,w,c,t2) >= p_cumDem(p_base,w,c,t) and p_dataAtende_ord(p_base,w,c,t) = 0),
        p_dataAtende_ord(p_base,w,c,t) = ord(t2);
    );
);

p_diasDesvio(p_base,w,c,t)$(p_dataAtende_ord(p_base,w,c,t) > 0) = p_dataAtende_ord(p_base,w,c,t) - ord(t);

* Escrita do CSV
put f_status_csv 'PN_Base,LarguraProduto,CompProduto,DataEntregaRequerida,QtdDemandada,DataProducaoReal,DiasDesvio,StatusEntrega' /;
Scalar s_rel_dias_desvio, s_rel_status_code;
loop((p_base,w,c,t)$ (p_demandaPwct(p_base,w,c,t) > 0),
    put f_status_csv p_base.tl, ',', w.tl, ',', c.tl, ',', t.tl, ',';
    put f_status_csv p_demandaPwct(p_base,w,c,t):0:2, ',';

    s_rel_status_code = -1;
    s_rel_dias_desvio = 0;

    if(p_dataAtende_ord(p_base,w,c,t) > 0,
        loop(t_prod$(ord(t_prod) = p_dataAtende_ord(p_base,w,c,t)),
            put f_status_csv t_prod.tl;
            s_rel_dias_desvio = ord(t_prod) - ord(t);
            s_rel_status_code = 0$(s_rel_dias_desvio <= 0) + 1$(s_rel_dias_desvio > 0);
        );
    );
    put f_status_csv ',', s_rel_dias_desvio:0:0, ',', s_rel_status_code:0:0 /;
);
putclose f_status_csv;
