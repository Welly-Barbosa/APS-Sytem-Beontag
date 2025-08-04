using APSSystem.Application.Interfaces;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSSystem.Infrastructure.GamsIntegration;

public class GamsFileWriter : IGamsFileWriter
{
    public async Task GerarArquivoDeEntradaAsync(string caminhoArquivo, GamsInputData dadosParaGams)
    {
        var sb = new StringBuilder();
        var culture = CultureInfo.InvariantCulture; // Garante que números decimais usem "."

        sb.AppendLine($"* Arquivo de dados gerado em: {DateTime.Now}");
        sb.AppendLine();

        // --- Etapa de Extração de Dados ---
        var produtosAgregados = dadosParaGams.DemandasAgregadas.Keys.Select(k => k.PartNumber).Distinct();
        var p_base_unicos = produtosAgregados.Select(p => p.PN_Generico).Distinct().OrderBy(p => p);
        var w_unicos = produtosAgregados.Select(p => p.Largura).Distinct().OrderBy(w => w);
        var c_unicos = produtosAgregados.Where(p => p.Comprimento.HasValue).Select(p => p.Comprimento!.Value).Distinct().OrderBy(c => c);
        var datasDoHorizonte = dadosParaGams.TempoDisponivelDiarioEmMinutos.Keys.Select(k => k.Data).Distinct().OrderBy(d => d);

        // --- DEFINIÇÃO DOS SETS ---

        sb.AppendLine("Set j 'recursos' /");
        foreach (var recurso in dadosParaGams.Recursos) sb.AppendLine($"  {recurso.Id}");
        sb.AppendLine("/;");
        sb.AppendLine();

        sb.AppendLine("Set t 'dias do horizonte de planejamento' /");
        foreach (var data in datasDoHorizonte) sb.AppendLine($"  '{data:yyyy-MM-dd}'");
        sb.AppendLine("/;");
        sb.AppendLine();

        sb.AppendLine("Set p_base 'produtos genericos' /");
        foreach (var p in p_base_unicos) sb.AppendLine($"  '{p}'");
        sb.AppendLine("/;");
        sb.AppendLine();

        sb.AppendLine("Set w 'larguras' /");
        foreach (var w in w_unicos) sb.AppendLine($"  '{w.ToString(culture)}'");
        sb.AppendLine("/;");
        sb.AppendLine();

        sb.AppendLine("Set c 'comprimentos' /");
        foreach (var c in c_unicos) sb.AppendLine($"  '{c.ToString(culture)}'");
        sb.AppendLine("/;");
        sb.AppendLine();

        sb.AppendLine("Set p(p_base, w, c) 'produtos agregados validos' /");
        foreach (var produto in produtosAgregados)
        {
            if (produto.Comprimento.HasValue)
            {
                sb.AppendLine($"  '{produto.PN_Generico}'.'{produto.Largura.ToString(culture)}'.'{produto.Comprimento.Value.ToString(culture)}'");
            }
        }
        sb.AppendLine("/;");
        sb.AppendLine();

        // --- PARÂMETROS ESCALARES ---
        sb.AppendLine("Scalars");
        sb.AppendLine($"    s_larguraMae            'Largura da bobina-mae em pol'      / {78.74.ToString(culture)} / ");
        sb.AppendLine($"    s_comprimentoMae_pes    'Comprimento da bobina-mae em pol'  / {15000.ToString(culture)} /;");
        sb.AppendLine();

        // --- PARÂMETROS DE RECURSO ---
        sb.AppendLine("Parameter p_velocidadeMaq(j) 'Velocidade da maquina em polegadas por minuto' /");
        foreach (var r in dadosParaGams.Recursos) sb.AppendLine($"    {r.Id} {r.VelocidadePolPorMinuto.ToString(culture)}");
        sb.AppendLine("/;");
        sb.AppendLine();

        sb.AppendLine("Parameter p_eficiencia(j) 'Eficiencia media da maquina (percentual)' /");
        foreach (var r in dadosParaGams.Recursos) sb.AppendLine($"    {r.Id} {r.Eficiencia.ToString(culture)}");
        sb.AppendLine("/;");
        sb.AppendLine();

        sb.AppendLine("Parameter p_tempoSetupBase(j) 'Tempo de setup em minutos para a maquina' /");
        foreach (var r in dadosParaGams.Recursos) sb.AppendLine($"    {r.Id} {r.TempoDeSetupEmMinutos.ToString(culture)}");
        sb.AppendLine("/;");
        sb.AppendLine();

        sb.AppendLine("Parameter p_maxCortes(j) 'Numero maximo de cortes simultaneos na maquina' /");
        foreach (var r in dadosParaGams.Recursos) sb.AppendLine($"    {r.Id} {r.MaximoCortes}");
        sb.AppendLine("/;");
        sb.AppendLine();

        sb.AppendLine("Parameter p_custoHora(j) 'Custo operacional da maquina por hora' /");
        foreach (var r in dadosParaGams.Recursos) sb.AppendLine($"    {r.Id} {r.CustoPorHora.ToString(culture)}");
        sb.AppendLine("/;");
        sb.AppendLine();

        // --- PARÂMETRO DE TEMPO DISPONÍVEL ---
        sb.AppendLine("Parameter p_tempoDisponivel(j,t) 'Tempo disponivel em minutos por dia' /");
        foreach (var tempo in dadosParaGams.TempoDisponivelDiarioEmMinutos)
        {
            sb.AppendLine($"    {tempo.Key.RecursoId}.'{tempo.Key.Data:yyyy-MM-dd}' {tempo.Value.ToString("F2", culture)}");
        }
        sb.AppendLine("/;");
        sb.AppendLine();

        // --- PARÂMETROS DE PRODUTO E DEMANDA ---
        sb.AppendLine("Parameter p_larguraProduto(p_base, w, c) 'Largura numerica de cada produto (redundante)' /");
        foreach (var p in produtosAgregados)
        {
            if (p.Comprimento.HasValue)
                sb.AppendLine($"    '{p.PN_Generico}'.'{p.Largura.ToString(culture)}'.'{p.Comprimento.Value.ToString(culture)}' {p.Largura.ToString(culture)}");
        }
        sb.AppendLine("/;");
        sb.AppendLine();

        sb.AppendLine("Parameter p_comprimentoProduto(p_base, w, c) 'Comprimento numerico de cada produto (redundante)' /");
        foreach (var p in produtosAgregados)
        {
            if (p.Comprimento.HasValue)
                sb.AppendLine($"    '{p.PN_Generico}'.'{p.Largura.ToString(culture)}'.'{p.Comprimento.Value.ToString(culture)}' {p.Comprimento.Value.ToString(culture)}");
        }
        sb.AppendLine("/;");
        sb.AppendLine();

        sb.AppendLine("Parameter p_demanda(p_base, w, c, t) 'Demanda agregada por produto e data de entrega' /");
        foreach (var demanda in dadosParaGams.DemandasAgregadas)
        {
            var pn = demanda.Key.PartNumber;
            var data = demanda.Key.DataEntrega;
            if (pn.Comprimento.HasValue)
            {
                string keyGams = $"'{pn.PN_Generico}'.'{pn.Largura.ToString(culture)}'.'{pn.Comprimento.Value.ToString(culture)}'.'{data:yyyy-MM-dd}'";
                sb.AppendLine($"  {keyGams}  {demanda.Value}");
            }
        }
        sb.AppendLine("/;");

        await File.WriteAllTextAsync(caminhoArquivo, sb.ToString());
    }
}