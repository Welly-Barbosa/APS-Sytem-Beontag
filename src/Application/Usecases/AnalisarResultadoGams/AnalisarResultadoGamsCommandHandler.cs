using APSSystem.Application.DTOs;
using APSSystem.Application.UseCases.AnalisarResultadoGams;
using APSSystem.Core.Entities;
using Domain.Entities;
using MediatR;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Application.Usecases.AnalisarResultadoGams
{
    /// <summary>
    /// Command handler para analisar os resultados do GAMS e gerar um DTO de produção.
    /// </summary>
    public class AnalisarResultadoGamsCommandHandler : IRequestHandler<AnalisarResultadoGamsCommand, List<ProducaoDto>>
    {
        /// <summary>
        /// Manipula o comando de análise de resultados do GAMS.
        /// </summary>
        /// <param name="request">O comando contendo o conteúdo do arquivo de resultado do GAMS.</param>
        /// <param name="cancellationToken">O token de cancelamento.</param>
        /// <returns>Uma lista de DTOs de produção representando o plano de produção otimizado.</returns>
        public Task<List<ProducaoDto>> Handle(AnalisarResultadoGamsCommand request, CancellationToken cancellationToken)
        {
            var producoes = new List<Producao>();
            var linhas = request.ConteudoArquivo.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Extrair o valor da função objetivo
            var linhaFuncaoObjetivo = linhas.FirstOrDefault(l => l.Contains("OBJECTIVE VALUE"));
            if (linhaFuncaoObjetivo != null)
            {
                var match = Regex.Match(linhaFuncaoObjetivo, @"\d+\.\d+");
                if (match.Success)
                {
                    double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var valorObjetivo);
                }
            }

            // Encontrar o início da seção de variáveis
            int indiceInicioVariaveis = -1;
            for (int i = 0; i < linhas.Length; i++)
            {
                if (linhas[i].Trim() == "---- VAR X")
                {
                    indiceInicioVariaveis = i + 1;
                    break;
                }
            }

            if (indiceInicioVariaveis != -1)
            {
                for (int i = indiceInicioVariaveis; i < linhas.Length; i++)
                {
                    if (linhas[i].Trim().StartsWith("----")) break;

                    var partes = linhas[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (partes.Length >= 4)
                    {
                        var identificador = partes[0];
                        double.TryParse(partes[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var nivel);

                        if (nivel > 0)
                        {
                            var ids = identificador.Split('.');
                            if (ids.Length == 2)
                            {
                                int.TryParse(ids[0], out var maquinaId);
                                int.TryParse(ids[1], out var produtoId);
                                producoes.Add(new Producao { MaquinaId = maquinaId, ProdutoId = produtoId, Quantidade = (int)nivel });
                            }
                        }
                    }
                }
            }

            var resultados = GamsResultParser.Parse(request.ConteudoArquivo);
            foreach (var item in resultados.Variables)
            {
                // Explicação da Lógica: Para cada variável no resultado do GAMS, tentamos converter seu nome
                // (que deve representar o ID da máquina) em um número inteiro.
                // Isso é necessário para comparar com o `MaquinaId` (int) da nossa entidade `Producao`.
                if (int.TryParse(item.Name, out int maquinaId))
                {
                    // Se a conversão for bem-sucedida, procuramos a produção correspondente usando o ID numérico.
                    var producao = producoes.FirstOrDefault(p => p.MaquinaId == maquinaId);
                    if (producao != null)
                    {
                        producao.HoraInicio = item.Level;
                        producao.Duracao = item.Marginal;
                    }
                }
            }

            var producaoDtos = producoes.Select(p => new ProducaoDto
            {
                MaquinaId = p.MaquinaId,
                ProdutoId = p.ProdutoId,
                Quantidade = p.Quantidade,
                HoraInicio = p.HoraInicio,
                Duracao = p.Duracao
            }).ToList();

            return Task.FromResult(producaoDtos);
        }
    }
}