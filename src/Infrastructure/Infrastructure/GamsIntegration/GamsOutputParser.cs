using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using APSSystem.Application.DTOs;
using APSSystem.Application.Interfaces;

namespace APSSystem.Infrastructure.GamsIntegration
{
    /// <summary>
    /// Implementação do parser de saída do GAMS.
    /// </summary>
    public sealed class GamsOutputParser : IGamsOutputParser
    {
        private static readonly CultureInfo culture = CultureInfo.InvariantCulture;

        public List<ProducaoDto> Parse(string conteudo)
        {
            if (string.IsNullOrWhiteSpace(conteudo))
                return new List<ProducaoDto>();

            var linhas = conteudo
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            if (!linhas.Any()) return new List<ProducaoDto>();

            var primeiraLinha = linhas.First();
            var separador = primeiraLinha.Contains(';') ? ';' : ',';

            if (TemCabecalho(primeiraLinha))
                linhas = linhas.Skip(1).ToList();

            var itens = new List<ProducaoDto>();
            foreach (var l in linhas)
            {
                var cols = l.Split(separador);
                var dto = new ProducaoDto
                {
                    Linha = GetOrEmpty(cols, 0),
                    Produto = GetOrEmpty(cols, 1),
                    Quantidade = ParseDouble(GetOrEmpty(cols, 2)),
                    Inicio = ParseDate(GetOrEmpty(cols, 3)),
                    Fim = ParseDate(GetOrEmpty(cols, 4))
                };
                itens.Add(dto);
            }
            return itens;
        }

        private static bool TemCabecalho(string linha)
        {
            var lower = linha.ToLowerInvariant();
            return lower.Contains("linha") || lower.Contains("produto");
        }

        private static string GetOrEmpty(string[] cols, int idx) =>
            (idx < cols.Length) ? cols[idx].Trim() : string.Empty;

        private static double ParseDouble(string s) =>
            double.TryParse(s, NumberStyles.Any, culture, out var v) ? v : 0d;

        private static DateTime? ParseDate(string s) =>
            DateTime.TryParse(s, culture, DateTimeStyles.AssumeLocal, out var dt) ? dt : null;
    }
}