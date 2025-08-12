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
Sets
    pt                    'Padroes de corte (gerados dinamicamente)' /pt0001*pt9999/
    c_unicos(c)           'Comprimentos unicos presentes nos produtos (gerado dinamicamente)'
    p_c(p_base, w, c)     'Filtro de produtos por comprimento para o loop principal';

* --- Cria um alias para o conjunto de tempo, necessario para o calculo de atraso
Alias(t, t_prod);
* --- NOVO ALIAS NECESSARIO PARA O LOOP DO RELATORIO ---
Alias(t, t_demand_loop);

* ------------------------------------------------------------------------------
* 1.3 Declaracao de Parametros Calculados e Escalares
* ------------------------------------------------------------------------------
Parameters
    p_rendimentoPadrao(pt, p_base, w, c) 'Qtd de itens (p) produzidos por 1 bobina-mae com o padrao (pt)'
    p_numCortes(pt)                      'Numero de cortes de um padrao'
    p_tempoTotalPadrao(pt, j)            'Tempo total (min) para processar 1 bobina-mae com padrao pt na maquina j'
    p_precoDual(p_base, w, c)            'Preco dual da restricao de demanda, usado pelo subproblema'
    p_custoAtraso(p_base, w, c, t)       'Penalidade por dia de atraso para cada necessidade'
    p_velocidadeMaq_fpm(j)               'Velocidade da maquina em pes por minuto';

Scalars
    s_timeFenceDias        / 3 /
    s_timeFenceMultiplier  / 1000 /
    s_pesoCustoProducao    / 100 /
    s_pesoCustoAtraso      / 10 /
    s_pesoCustoExcedente   / 0.1 /
    s_pesoCustoFalta       / 10000000 /
* --- NOVOS PARAMETROS PARA PREENCHIMENTO DE CAPACIDADE ---
    s_diasParaPreencher    / 3 /
*'Numero de dias iniciais cuja capacidade deve ser preenchida'
    s_percMinUtilizacao    / 0.80 /;
*'Percentual minimo de utilizacao da capacidade nesse periodo';
* ------------------------------------------------------------------------------
* FASE 1.4: DECLARACOES PARA RELATORIOS CSV (GEEK - VERSAO FINAL)
* ------------------------------------------------------------------------------

* --- Declaracao dos arquivos de saida para o sistema APS
File
    f_composicao_csv    '/relatorio_composicao_padroes.csv/'
    f_plano_csv         '/relatorio_plano_producao.csv/'
    f_status_csv        '/relatorio_status_entregas.csv/';

* --- Declaracao de parametros auxiliares para o relatorio de status de entrega
Parameters
    p_rel_producao_total(p_base,w,c,t)   'Auxiliar: Producao total de cada item por dia'
    p_rel_demanda_restante(p_base,w,c,t) 'Auxiliar: Controle de demanda para o relatorio'
    p_rel_data_real(p_base,w,c,t)        'Auxiliar: Data de producao que atendeu a demanda (ord)'
    p_rel_dias_desvio(p_base,w,c,t)      'Auxiliar: Dias de desvio (positivo=atraso, negativo=adiantado)';

* --- Declaracao de escalares temporarios para os loops de relatorio
Scalars
    s_rel_qtd_atendida 'Variavel temporaria para loop'
    s_rel_dias_desvio  'Variavel temporaria para loop'
    s_rel_status_code  'Variavel temporaria para loop - Codigo de Status da Entrega';
    
* ==============================================================================
* FASE 2: CALCULOS PREPARATORIOS E VALIDACAO
* ==============================================================================

* Popula o conjunto c_unicos dinamicamente com base nos comprimentos que
* realmente existem nos produtos definidos no Set p.
c_unicos(c) = yes$sum((p_base, w), p(p_base, w, c));

* Conversao de velocidade de pol/min para pes/min para consistencia com o comprimento
p_velocidadeMaq_fpm(j) = p_velocidadeMaq(j) * 60;

* ------------------------------------------------------------------------------
* INICIO DO BLOCO DE AUDITORIA E SANIDADE DOS DADOS (GEEK - VERSAO ROBUSTA)
* ------------------------------------------------------------------------------

* --- Verificacoes existentes (Boas Praticas!)
if (s_comprimentoMae_pes <= 0, ABORT "Comprimento da bobina-mae (s_comprimentoMae_pes) deve ser positivo.");
Set j_com_erro(j);
j_com_erro(j) = yes$(p_velocidadeMaq_fpm(j) <= 0);
if (card(j_com_erro) > 0,
    display "ERRO: Velocidade de maquina invalida (<= 0) para:", j_com_erro;
    ABORT "Corrija os dados de entrada no arquivo .dat.";
);

* --- Auditoria 1 (Revisada): Coerencia entre os indices e os parametros de dimensao
* Por que esta verificacao e importante?
* O modelo depende que o rotulo do conjunto 'w' e 'c' corresponda exatamente
* ao valor numerico nos parametros p_larguraProduto e p_comprimentoProduto.
* Uma inconsistencia aqui levaria a calculos de corte e tempo totalmente errados.
* Esta versao e mais robusta pois define explicitamente o dominio (p_base, w, c)
* para a atribuicao, eliminando qualquer ambiguidade.
Set
    p_erro_largura(p_base, w, c)     'Produtos com inconsistencia na largura'
    p_erro_comprimento(p_base, w, c) 'Produtos com inconsistencia no comprimento';

p_erro_largura(p_base, w, c)$(p(p_base, w, c) and p_larguraProduto(p_base, w, c) <> w.val) = yes;
p_erro_comprimento(p_base, w, c)$(p(p_base, w, c) and p_comprimentoProduto(p_base, w, c) <> c.val) = yes;

if(card(p_erro_largura) > 0,
    display "ERRO DE COERENCIA: A largura no indice do produto (w) nao bate com o parametro p_larguraProduto para:", p_erro_largura;
    ABORT "Corrija a inconsistencia nos dados de entrada.";
);
if(card(p_erro_comprimento) > 0,
    display "ERRO DE COERENCIA: O comprimento no indice do produto (c) nao bate com o parametro p_comprimentoProduto para:", p_erro_comprimento;
    ABORT "Corrija a inconsistencia nos dados de entrada.";
);


* --- Auditoria 2: Viabilidade da Largura do Produto
* Por que esta verificacao e importante?
* Se um produto for mais largo que a bobina-mae, nenhum padrao de corte
* podera ser gerado para ele, garantindo demanda nao atendida (v_falta).
Set p_erro_largura_mae(p_base, w, c);
p_erro_largura_mae(p_base, w, c)$(p(p_base, w, c) and p_larguraProduto(p_base, w, c) > s_larguraMae) = yes;

if(card(p_erro_largura_mae) > 0,
    display "ERRO DE VIABILIDADE: Produtos mais largos que a bobina-mae encontrados:", p_erro_largura_mae;
    ABORT "Esses produtos nunca poderao ser produzidos. Verifique os dados.";
);

* --- Auditoria 3: Validacao de Dominio dos Parametros
* Por que esta verificacao e importante?
* Garante que os dados de entrada estao dentro dos seus valores esperados
* (e.g., sem demandas ou tempos negativos que causariam comportamento inesperado).
if(sum((p,t)$(p_demanda(p,t) < 0), 1),
    Abort "VIOLACAO DE DADOS: Existem valores de demanda negativos no arquivo de entrada.";
);
if(sum((j,t)$(p_tempoDisponivel(j,t) < 0), 1),
    Abort "VIOLACAO DE DADOS: Existem valores de tempo disponivel negativos no arquivo de entrada.";
);


* --- Auditoria 4: Consistencia Agregada (Capacidade vs. Demanda)
* Por que esta verificacao e importante?
* Compara uma estimativa do tempo total necessario para produzir tudo
* contra o tempo total disponivel. Se a necessidade for muito maior que a
* disponibilidade, o modelo provavelmente sera inviavel ou tera alto custo de falta.
* NOTA: Este e um calculo aproximado, pois ignora o tempo de setup e a eficiencia
* para uma verificacao rapida. Usa a velocidade da maquina mais rapida como referencia.
Scalars
    s_tempoTotalDisponivel  'Tempo total disponivel em todas as maquinas no horizonte (min)'
    s_tempoTotalRequerido   'Estimativa de tempo para atender toda a demanda (min)'
    s_velocidadeRef_fpm     'Velocidade da maquina mais rapida (pes/min) para referencia';

s_tempoTotalDisponivel = sum((j,t), p_tempoDisponivel(j,t));
s_velocidadeRef_fpm = smax(j, p_velocidadeMaq_fpm(j));

if (s_velocidadeRef_fpm > 0,
*    s_tempoTotalRequerido = sum(p_demanda(p,t), p_demanda(p,t) * (p_comprimentoProduto(p_demanda) / s_velocidadeRef_fpm) );
    s_tempoTotalRequerido = sum((p,t)$p_demanda(p,t), p_demanda(p,t) * (p_comprimentoProduto(p) / s_velocidadeRef_fpm));
*    s_tempoTotalRequerido = sum((p, t), p_demanda(p,t) * (p_comprimentoProduto(p) / s_velocidadeRef_fpm));    
    display "[AUDITORIA] Tempo Total Disponivel (min):", s_tempoTotalDisponivel;
    display "[AUDITORIA] Tempo Total Requerido (min, estimado):", s_tempoTotalRequerido;

    if (s_tempoTotalRequerido > s_tempoTotalDisponivel,
        display "ALERTA DE CAPACIDADE: O tempo requerido estimado excede o tempo total disponivel. O modelo pode ter alto custo de falta ou ser infactivel.";
    );
);

* ------------------------------------------------------------------------------
* FIM DO BLOCO DE AUDITORIA
* ------------------------------------------------------------------------------


* Calcula a penalidade de atraso para cada ponto de demanda.
p_custoAtraso(p,t)$p_demanda(p,t) = 1 / (ord(t) + 1e-6);
p_custoAtraso(p,t)$(p_demanda(p,t) and ord(t) <= s_timeFenceDias) = p_custoAtraso(p,t) * s_timeFenceMultiplier;

* ==============================================================================
* FASE 3: DECLARACAO DOS MODELOS DE OTIMIZACAO (GERACAO DE COLUNAS)
* ==============================================================================

* ------------------------------------------------------------------------------
* 3.1 Variaveis Globais
* ------------------------------------------------------------------------------
Variables
    v_usaPadrao(pt, j, t)
    v_excedente(p_base, w, c)
    v_diasAtraso(p_base, w, c, t)
    v_falta(p_base, w, c)
    z_custoTotal;

Integer Variable v_usaPadrao 'Numero de bobinas-mae processadas com o padrao pt na maquina j no dia t';
Positive Variable
    v_excedente 'Quantidade produzida do produto p acima da sua demanda total'
    v_diasAtraso 'Mede o atraso ponderado para cada necessidade (p,t)'
    v_falta 'Quantidade de demanda total do produto p que nao foi atendida';
Variable z_custoTotal 'Valor total da funcao objetivo a ser minimizado';

* ------------------------------------------------------------------------------
* 3.2 Problema Mestre (Restrito e Final)
* ------------------------------------------------------------------------------
Equations
    eq_objetivo
    eq_atendeDemanda(p_base, w, c)
    eq_calculaAtraso(p_base, w, c, t)
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
    sum((pt,j,t), p_rendimentoPadrao(pt, p) * v_usaPadrao(pt,j,t)) + v_falta(p)
    =e= sum(t, p_demanda(p, t)) + v_excedente(p);

* O atraso e calculado para cada necessidade especifica se a producao ocorrer apos o prazo.
eq_calculaAtraso(p,t)$p_demanda(p,t)..
    v_diasAtraso(p,t) =g= sum((pt,j,t_prod)$(ord(t_prod) > ord(t)),
                             (ord(t_prod) - ord(t)) * p_rendimentoPadrao(pt, p) * v_usaPadrao(pt,j,t_prod) );

* O tempo total de producao e setup em uma maquina/dia nao pode exceder o tempo disponivel.
eq_capacidade(j, t)..
    sum(pt, p_tempoTotalPadrao(pt,j) * v_usaPadrao(pt,j,t)) =l= p_tempoDisponivel(j,t);
    
* Forca o uso minimo da capacidade disponivel nos primeiros 'N' dias.
eq_preencheCapacidade(j)..
    sum((pt, t)$(ord(t) <= s_diasParaPreencher), p_tempoTotalPadrao(pt, j) * v_usaPadrao(pt, j, t))
    =g= s_percMinUtilizacao * sum(t$(ord(t) <= s_diasParaPreencher), p_tempoDisponivel(j,t));
    


Model CorteMestre_RMIP /eq_objetivo, eq_atendeDemanda, eq_calculaAtraso, eq_capacidade, eq_preencheCapacidade/;
Model CorteMestre_Final /CorteMestre_RMIP/;

* ------------------------------------------------------------------------------
* 3.3 Subproblema (Gerador de Padroes)
* ------------------------------------------------------------------------------
Variables
    v_geraCorte(p_base, w, c), z_valorPadrao;
Integer Variable v_geraCorte;
Variable z_valorPadrao;

Equations
    eq_sub_objetivo, eq_sub_larguraBobina;

* O objetivo do subproblema e maximizar o valor do padrao com base nos precos duais.
eq_sub_objetivo.. z_valorPadrao =e= sum(p_c, p_precoDual(p_c) * v_geraCorte(p_c));

* A soma das larguras dos itens no padrao nao pode exceder a largura da bobina-mae.
eq_sub_larguraBobina.. sum(p_c, p_larguraProduto(p_c) * v_geraCorte(p_c)) =l= s_larguraMae;

Model GeraPadrao /eq_sub_objetivo, eq_sub_larguraBobina/;

* ==============================================================================
* FASE 4: EXECUCAO DO ALGORITMO DE GERACAO DE COLUNAS
* ==============================================================================

Scalar
    s_contadorPadroes   'Contador de padroes gerados'
    s_podeMelhorar      'Flag de controle do loop: 1 = continua, 0 = para'
* --- CONTROLES EXPLICITOS DE TEMPO E ITERACAO ---
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
p_numCortes(pt) = 0;

s_contadorPadroes = 0;
s_timeStart = jnow;

* --- Preparacao segura de p_c (parametro 0/1) ---
* Se p_c ainda nao estiver preenchido nesta fase, derive a partir da demanda
* (evita erro 66 no subproblema: simbolo p_c sem valores)
*p_c como parametro 0/1:
p_c(p_base,w,c) = 0;
p_c(p_base,w,c)$(sum(t, p_demanda(p_base,w,c,t)) > 0) = 1;

* --- INICIO DA ESTRATEGIA DE WARM-UP (GEEK) ---
* Para a fase de geracao de colunas, usamos pesos mais balanceados
* para garantir que os precos duais sejam estaveis e permitam a
* criacao de bons padroes de corte.
put_utility 'log' / '[INFO] Usando pesos de WARM-UP para a geracao de colunas.';
s_pesoCustoProducao= 1;
s_pesoCustoAtraso = 10;
s_pesoCustoExcedente= 0.1;
s_pesoCustoFalta= 10000;

*   Acao: Relaxar o gap de otimalidade para 2% para o problema mestre
Option optcr = 0.02;

s_podeMelhorar = 1;
WHILE( (s_podeMelhorar = 1)
       and (s_iter < s_iterMax)
       and (s_semMelhora < s_semMelhoraMax)
       and ( (jnow - s_timeStart)*86400 < s_tempoMaximoTotal ),

    s_iter = s_iter + 1;

    p_tempoTotalPadrao(pt, j)$(s_contadorPadroes >= ord(pt) and p_velocidadeMaq_fpm(j) > 0) =
        p_tempoSetupBase(j) + (s_comprimentoMae_pes / p_velocidadeMaq_fpm(j));

    CorteMestre_RMIP.reslim = 60;
    CorteMestre_RMIP.optcr = 0.02;
    SOLVE CorteMestre_RMIP using RMIP minimizing z_custoTotal;
    p_precoDual(p_base,w,c) = eq_atendeDemanda.m(p_base,w,c);

* Monitoramento do progresso da Geração de Colunas
    put_utility 'log' / '[MONITORAMENTO] Iteracao ', s_iter:0:0, ' | Custo Mestre: ', z_custoTotal.l:0:6;

* Controle de progresso: contagem de iteracoes sem melhora relevante no mestre
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
        v_geraCorte.up(p_base,w,c) = p_maxCortes(j);
        
        GeraPadrao.reslim = 120;
        GeraPadrao.optcr  = 0.02;
        SOLVE GeraPadrao using MIP maximizing z_valorPadrao;
        
* Monitoramento do valor do padrão encontrado
        if(z_valorPadrao.l > 1 + s_epsPreco,
            put_utility 'log' / '[MONITORAMENTO] j=', j.tl, ' | Valor do Padrao: ', z_valorPadrao.l:0:6;
        );

* Candidato de cortes (arredondado) e checagem de duplicidade com o ultimo padrao
        p_candidato(p_base,w,c) = round(v_geraCorte.l(p_base,w,c));
        if( (z_valorPadrao.l > (1 + s_epsPreco)) and (sum((p_base,w,c)$p_c(p_base,w,c), abs(p_candidato(p_base,w,c) - p_lastPadrao(p_base,w,c))) > 0),
            s_podeMelhorar  = 1;
            s_contadorPadroes = s_contadorPadroes + 1;
            p_rendimentoPadrao(pt, p_base,w,c)$(ord(pt) = s_contadorPadroes and p_c(p_base,w,c)) = p_candidato(p_base,w,c);
            p_numCortes(pt)$(ord(pt) = s_contadorPadroes) = sum((p_base,w,c)$p_c(p_base,w,c), p_rendimentoPadrao(pt, p_base,w,c));
            p_lastPadrao(p_base,w,c) = p_candidato(p_base,w,c);
        );
    );

* Ajuste dinamico da tolerancia caso haja muitas iteracoes sem melhora
    if( s_semMelhora >= 10 and s_epsPreco < 0.02,
        s_epsPreco = 0.02;
        put_utility 'log' / '[MONITORAMENTO] Aumentando tolerancia de preco para ', s_epsPreco:0:3;
    );
);

* ==============================================================================







* ==============================================================================
* FASE 5: SOLUCAO FINAL E GERACAO DE RELATORIOS
* ==============================================================================
 
* --- RESTAURANDO OS PESOS ESTRATEGICOS FINAIS (GEEK) ---
* Agora que temos um bom conjunto de padroes, aplicamos a hierarquia
* de prioridades final para a selecao da solucao.
display "[INFO] Restaurando pesos ESTRATEGICOS para o solve final.";
s_pesoCustoProducao   = 100;    
s_pesoCustoAtraso      = 10;     
s_pesoCustoExcedente   = 0.1;     
s_pesoCustoFalta       = 50000 ; 
* --------------------------------------------------

CorteMestre_Final.reslim = 300;
CorteMestre_Final.optcr = 0.01;
option solPrint = on;
SOLVE CorteMestre_Final using MIP minimizing z_custoTotal;

* ==============================================================================
* FASE 5.1: RELATORIOS GERENCIAIS (GEEK)
* ==============================================================================
display "EXECUCAO CONCLUIDA.", z_custoTotal.l;
* --- Declaração de parâmetros para armazenar os componentes do custo
Scalars
    s_rel_custoProducao   'Componente do Custo: Custo total de producao (bobinas-mae)'
    s_rel_custoAtraso     'Componente do Custo: Custo total de atraso'
    s_rel_custoExcedente  'Componente do Custo: Custo total de excedente'
    s_rel_custoFalta      'Componente do Custo: Custo total de falta';

* --- Cálculo dos componentes do custo para o relatório
s_rel_custoProducao  = s_pesoCustoProducao * sum((pt,j,t), v_usaPadrao.l(pt,j,t));
s_rel_custoAtraso    = s_pesoCustoAtraso   * sum((p,t), p_custoAtraso(p,t) * v_diasAtraso.l(p,t));
s_rel_custoExcedente = s_pesoCustoExcedente* sum(p, v_excedente.l(p));
s_rel_custoFalta     = s_pesoCustoFalta    * sum(p, v_falta.l(p));

* --- Exibição dos Relatórios no arquivo .LST

display "================================================================";
display "========= RELATORIO GERENCIAL - OTIMIZACAO DE CORTE ============";
display "================================================================";

display "[1] SUMARIO DA EXECUCAO";
display "   - Status do Modelo:", CorteMestre_Final.modelStat;
display "   - Status do Solver:", CorteMestre_Final.solveStat;
display "   - Custo Total da Solucao:", z_custoTotal.l;
display "   - Numero de Padroes Gerados:", s_contadorPadroes;

display "[2] COMPOSICAO DO CUSTO TOTAL";
display "   - Custo de Producao (Bobinas-mae):", s_rel_custoProducao;
display "   - Custo de Atraso (Ponderado):"   , s_rel_custoAtraso;
display "   - Custo de Excedente:"            , s_rel_custoExcedente;
display "   - Custo de Falta (Demanda Nao Atendida):", s_rel_custoFalta;

display "[3] ATENDIMENTO DA DEMANDA";
display "   - Itens com Falta (Demanda Nao Atendida):", v_falta.l;
display "   - Itens com Excedente de Producao:", v_excedente.l;

* --- Parâmetros para relatórios de utilização
Parameters
    p_rel_tempoUtilizado(j,t)   'Tempo utilizado na maquina j no dia t'
    p_rel_percUtilizacao(j,t)   'Percentual de utilizacao da maquina j no dia t';

p_rel_tempoUtilizado(j,t) = sum(pt, p_tempoTotalPadrao(pt,j) * v_usaPadrao.l(pt,j,t));
p_rel_percUtilizacao(j,t)$(p_tempoDisponivel(j,t) > 0) = p_rel_tempoUtilizado(j,t) / p_tempoDisponivel(j,t);

display "[4] UTILIZACAO DA CAPACIDADE DAS MAQUINAS (%)";
display "   (Apenas dias com capacidade > 0 e utilizacao > 0 sao mostrados)";
display p_rel_percUtilizacao;

display "[5] PLANO DE PRODUCAO (PADROES UTILIZADOS)";
display "   (Mostra quantas bobinas-mae de cada padrao usar, por maquina e dia)";
display v_usaPadrao.l;

display "================================================================";
display "====================== FIM DO RELATORIO ========================";
display "================================================================";

* ==============================================================================
* FASE 5.2: GERACAO DE ARQUIVOS CSV PARA APS (CONTRATO DE DADOS)
* ==============================================================================
*--- FASE 5.2 - Geracao dos arquivos CSV conforme contrato ---*

* Inicializacoes de seguranca para evitar erro 141 (simbolos sem valor)
* Caso ja sejam inicializados em outra fase, isto apenas garante defaults
p_rel_data_real(p_base,w,c,t) = 0;
s_rel_dias_desvio = 0;
s_rel_status_code = -1;

* f_composicao_csv
put f_composicao_csv 'PadraoCorte,PN_Base,LarguraProduto,CompProduto,QtdPorBobinaMae' /;
loop((pt,p_base,w,c)$p_rendimentoPadrao(pt,p_base,w,c),
    put f_composicao_csv pt.tl, ',', p_base.tl, ',', w.tl, ',', c.tl, ',';
    put f_composicao_csv p_rendimentoPadrao(pt,p_base,w,c):0:2 /;
);
putclose f_composicao_csv;

* f_plano_csv
put f_plano_csv 'DataProducao,Maquina,PadraoCorte,QtdBobinasMae' /;
loop((pt,j,t)$(v_usaPadrao.l(pt,j,t) > 0.001),
    put f_plano_csv t.tl, ',', j.tl, ',', pt.tl, ',';
    put f_plano_csv v_usaPadrao.l(pt,j,t):0:2 /;
);
putclose f_plano_csv;

* f_status_csv
put f_status_csv 'PN_Base,LarguraProduto,CompProduto,DataEntregaRequerida,QtdDemandada,DataProducaoReal,DiasDesvio,StatusEntrega' /;
loop((p_base,w,c,t_demand_loop)$p_demanda(p_base,w,c,t_demand_loop),
    put f_status_csv p_base.tl, ',', w.tl, ',', c.tl, ',', t_demand_loop.tl, ',';
    put f_status_csv p_demanda(p_base,w,c,t_demand_loop):0:2, ',';

* DataProducaoReal: imprime se houver mapeamento; caso contrario, permanece vazio
    if(p_rel_data_real(p_base,w,c,t_demand_loop) > 0,
        loop(t_prod$(ord(t_prod) = round(p_rel_data_real(p_base,w,c,t_demand_loop))),
            put f_status_csv t_prod.tl;
        );
    );
    put f_status_csv ',';

* DiasDesvio (inteiro) e StatusEntrega (inteiro)
    put f_status_csv s_rel_dias_desvio:0:0, ',', s_rel_status_code:0:0 /;
);
putclose f_status_csv;
