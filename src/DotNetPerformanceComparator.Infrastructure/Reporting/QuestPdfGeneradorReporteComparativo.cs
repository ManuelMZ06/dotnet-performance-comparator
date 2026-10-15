using System.Globalization;
using DotNetPerformanceComparator.Application.Evaluations.Comparison;
using DotNetPerformanceComparator.Application.Evaluations.Reporting;
using DotNetPerformanceComparator.Application.Evaluations.Results;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DotNetPerformanceComparator.Infrastructure.Reporting;

/// <summary>
/// Generador del reporte comparativo PDF (CU-09) con QuestPDF. Construye el documento en memoria a partir de
/// los datos ya calculados por CU-05/CU-07 (no recalcula, no accede a BD/filesystem/NBomber). Stateless y
/// determinista salvo la fecha de generación. QuestPDF solo vive en esta capa.
/// </summary>
public sealed class QuestPdfGeneradorReporteComparativo : IGeneradorReporteComparativo
{
    private const int TamanoMetricaId = 6;
    private const string ColorNet8 = "#3b82f6";   // azul
    private const string ColorNet10 = "#22c55e";  // verde

    public ReportePdfGenerado Generar(
        EvaluacionDetalleConsulta detalle,
        ComparacionResumen comparacion,
        ComparacionEstres? estres,
        ReporteComparativoOpciones opciones)
    {
        var documento = new ReporteDocument(detalle, comparacion, estres, opciones);
        var contenido = documento.GeneratePdf();
        var nombre = $"comparacion-evaluacion-{detalle.Id}-{opciones.FechaGeneracion:yyyyMMdd-HHmmss}.pdf";
        return new ReportePdfGenerado(nombre, contenido);
    }

    // =============================================================== documento

    private sealed class ReporteDocument : IDocument
    {
        private readonly EvaluacionDetalleConsulta _d;
        private readonly ComparacionResumen _c;
        private readonly ComparacionEstres? _estres;
        private readonly ReporteComparativoOpciones _o;

        public ReporteDocument(EvaluacionDetalleConsulta d, ComparacionResumen c, ComparacionEstres? estres, ReporteComparativoOpciones o)
        {
            _d = d;
            _c = c;
            _estres = estres;
            _o = o;
        }

        private bool EsEstres => _estres is not null;

        private bool HayComparacion =>
            _c.Metricas.Any(EsGraficable) ||
            (_estres?.Niveles.Any(n => n.NivelComparable) ?? false);

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.6f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor("#1f2937"));

                page.Header().Element(Encabezado);
                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Spacing(14);

                    if (_o.IncluirDatosGenerales)
                    {
                        col.Item().Element(DatosGenerales);
                    }

                    if (!HayComparacion && (_o.IncluirTablaComparativa || _o.IncluirPromediosConsolidado || _o.IncluirGraficos))
                    {
                        col.Item().Element(SinComparacion);
                    }
                    else
                    {
                        if (_o.IncluirPromediosConsolidado)
                        {
                            col.Item().Element(c => Seccion(c, "Promedios y consolidado", PromediosTabla));
                        }

                        if (_o.IncluirTablaComparativa)
                        {
                            col.Item().Element(c => Seccion(c, EsEstres ? "Comparación .NET 8 vs .NET 10 (nivel máximo)" : "Comparación .NET 8 vs .NET 10", ComparativaTabla));
                            if (EsEstres)
                            {
                                col.Item().Element(c => Seccion(c, "Comparación por nivel de carga", NivelesTabla));
                                col.Item().Element(c => Seccion(c, "Contexto de saturación observado", Saturacion));
                            }
                        }

                        if (_o.IncluirGraficos)
                        {
                            col.Item().Element(Graficos);
                        }
                    }

                    if (_o.IncluirResultadosPorEjecucion)
                    {
                        col.Item().Element(c => Seccion(c, "Resultados por ejecución", EjecucionesTabla));
                    }

                    if (_o.IncluirConclusionObservaciones)
                    {
                        col.Item().Element(c => Seccion(c, "Conclusión y observaciones", Conclusion));
                    }
                });

                page.Footer().Element(Pie);
            });
        }

        // --------------------------------------------------------- encabezado / pie

        private void Encabezado(IContainer c) => c.Column(col =>
        {
            col.Item().Background("#1f2937").Padding(10).Column(h =>
            {
                h.Item().Text(_o.Titulo.ToUpperInvariant()).FontSize(15).Bold().FontColor("#ffffff");
                h.Item().Text(".NET 8 (referencia) vs .NET 10 (experimental)").FontSize(9).FontColor("#d1d5db");
            });
            col.Item().PaddingTop(4).Text($"Generado: {_o.FechaGeneracion:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#6b7280");
        });

        private void Pie(IContainer c) => c.Row(row =>
        {
            row.RelativeItem().Text($"Generado: {_o.FechaGeneracion:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#6b7280");
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(8).FontColor("#6b7280"));
                t.Span("Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        });

        // --------------------------------------------------------- secciones genéricas

        private static void Seccion(IContainer c, string titulo, Action<IContainer> cuerpo) => c.Column(col =>
        {
            col.Item().PaddingBottom(4).Text(titulo).FontSize(12).Bold().FontColor("#111827");
            col.Item().Element(cuerpo);
        });

        private void SinComparacion(IContainer c) => c.Border(1).BorderColor("#e5e7eb").Padding(10)
            .Text("No existen resultados comparables para esta evaluación.").FontColor("#92400e");

        // --------------------------------------------------------- datos generales

        private void DatosGenerales(IContainer c) => Seccion(c, "Datos generales", cuerpo => cuerpo.Column(col =>
        {
            void Fila(string k, string v) => col.Item().Row(r =>
            {
                r.ConstantItem(170).Text(k).SemiBold();
                r.RelativeItem().Text(v);
            });

            Fila("Evaluación", _d.Nombre);
            Fila("Proyecto", _d.Proyecto);
            if (_d.Migracion is { } m) Fila("Migración .NET 10", m);
            Fila("Tipo de prueba", _d.TipoPrueba);
            Fila("Estado", _d.Estado);
            Fila("Fecha de creación", _d.FechaCreacion.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture));
            Fila("Ejecuciones configuradas", _d.NumeroEjecuciones.ToString(CultureInfo.InvariantCulture));
            Fila("Endpoint", $"GET {_d.RutaObjetivo}");
            Fila("Métricas seleccionadas", string.Join(", ", _d.MetricasSeleccionadas.Select(x => x.Nombre)));

            var lineas = ParametrosReporte.Lineas(_d.Parametros);
            if (lineas.Count > 0)
            {
                col.Item().PaddingTop(6).Text("Parámetros de la prueba").SemiBold();
                foreach (var (etiqueta, valor) in lineas)
                {
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(170).PaddingLeft(10).Text(etiqueta);
                        r.RelativeItem().Text(valor);
                    });
                }
            }
        }));

        // --------------------------------------------------------- tablas comparativas

        private void PromediosTabla(IContainer c)
        {
            var filas = _c.Metricas;
            if (filas.Count == 0) { c.Text("Sin métricas.").Italic(); return; }

            c.Table(t =>
            {
                t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(2); cd.RelativeColumn(2); cd.RelativeColumn(2); });
                t.Header(h =>
                {
                    EncabezadoCelda(h.Cell(), "Métrica"); EncabezadoCelda(h.Cell(), ".NET 8");
                    EncabezadoCelda(h.Cell(), ".NET 10"); EncabezadoCelda(h.Cell(), "Pares");
                });
                foreach (var m in filas)
                {
                    Celda(t.Cell(), $"{m.Nombre} ({m.Unidad})");
                    Celda(t.Cell(), Valor(m.PromedioNet8, m.Unidad));
                    Celda(t.Cell(), Valor(m.PromedioNet10, m.Unidad));
                    Celda(t.Cell(), ParesTexto(m));
                }
            });
        }

        private void ComparativaTabla(IContainer c)
        {
            if (EsEstres && !_c.EsComparable)
            {
                c.Text("No existe una comparación de resumen válida al nivel máximo.").FontColor("#92400e");
                return;
            }

            TablaMetricas(c, _c.Metricas);
        }

        private void NivelesTabla(IContainer c)
        {
            if (_estres is null || _estres.Niveles.Count == 0) { c.Text("Sin niveles registrados.").Italic(); return; }

            c.Column(col =>
            {
                col.Spacing(8);
                foreach (var nivel in _estres.Niveles)
                {
                    col.Item().Column(ncol =>
                    {
                        ncol.Item().Text($"{nivel.UsuariosObjetivo} usuarios — {(nivel.NivelComparable ? "comparable" : "no comparable")}").SemiBold();
                        if (nivel.NotaNivel is { } nota) ncol.Item().Text(nota).FontSize(8).Italic().FontColor("#6b7280");
                        if (nivel.NivelComparable) ncol.Item().PaddingTop(2).Element(cc => TablaMetricas(cc, nivel.Metricas));
                    });
                }
            });
        }

        private void TablaMetricas(IContainer c, IReadOnlyList<ComparacionMetrica> metricas)
        {
            if (metricas.Count == 0) { c.Text("Sin métricas.").Italic(); return; }

            c.Table(t =>
            {
                t.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(3); cd.RelativeColumn(2); cd.RelativeColumn(2);
                    cd.RelativeColumn(2); cd.RelativeColumn(2); cd.RelativeColumn(2); cd.RelativeColumn(2);
                });
                t.Header(h =>
                {
                    EncabezadoCelda(h.Cell(), "Métrica"); EncabezadoCelda(h.Cell(), ".NET 8"); EncabezadoCelda(h.Cell(), ".NET 10");
                    EncabezadoCelda(h.Cell(), "Diferencia"); EncabezadoCelda(h.Cell(), "Variación %");
                    EncabezadoCelda(h.Cell(), "Interpretación"); EncabezadoCelda(h.Cell(), "Pares");
                });
                foreach (var m in metricas)
                {
                    Celda(t.Cell(), $"{m.Nombre} ({m.Unidad})");
                    Celda(t.Cell(), Valor(m.PromedioNet8, m.Unidad));
                    Celda(t.Cell(), Valor(m.PromedioNet10, m.Unidad));
                    Celda(t.Cell(), Diferencia(m));
                    Celda(t.Cell(), Porcentaje(m.DiferenciaPorcentual));
                    Celda(t.Cell(), InterpretacionTexto(m.Interpretacion));
                    Celda(t.Cell(), ParesTexto(m));
                }
            });
        }

        private void Saturacion(IContainer c) => c.Column(col =>
        {
            col.Item().Text("Describe únicamente el experimento realizado; no representa capacidad máxima ni una séptima métrica.")
                .FontSize(8).Italic().FontColor("#6b7280");
            if (_estres is { } e)
            {
                col.Item().PaddingTop(4).Text($".NET 8 — {ContextoTexto(e.Contexto.Net8)}");
                col.Item().Text($".NET 10 — {ContextoTexto(e.Contexto.Net10)}");
            }
        });

        private static string ContextoTexto(ContextoSaturacionVersion v)
        {
            var max = v.MaxUsuariosCompletados is { } mc ? $"Máximo nivel completado observado: {mc} usuarios." : "No se completó ningún nivel.";
            string sat;
            if (!v.Saturo) sat = "No se observó saturación.";
            else sat = v.NivelSaturacionMinObservado == v.NivelSaturacionMaxObservado
                ? $"Saturación observada: {v.NivelSaturacionMinObservado} usuarios."
                : $"Saturación observada: entre {v.NivelSaturacionMinObservado} y {v.NivelSaturacionMaxObservado} usuarios.";
            return $"{max} {sat}";
        }

        // --------------------------------------------------------- ejecuciones

        private void EjecucionesTabla(IContainer c)
        {
            if (_d.Ejecuciones.Count == 0) { c.Text("Sin ejecuciones registradas.").Italic(); return; }

            c.Table(t =>
            {
                t.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.RelativeColumn(1); cd.RelativeColumn(2); cd.RelativeColumn(5); });
                t.Header(h =>
                {
                    EncabezadoCelda(h.Cell(), "Versión"); EncabezadoCelda(h.Cell(), "Rep.");
                    EncabezadoCelda(h.Cell(), "Estado"); EncabezadoCelda(h.Cell(), "Resultados / condiciones");
                });
                foreach (var e in _d.Ejecuciones)
                {
                    Celda(t.Cell(), e.VersionDotNet);
                    Celda(t.Cell(), e.NumeroEjecucion.ToString(CultureInfo.InvariantCulture));
                    Celda(t.Cell(), EstadoEjecucion(e.Estado));
                    Celda(t.Cell(), DescripcionEjecucion(e));
                }
            });
        }

        private static string DescripcionEjecucion(EjecucionConsulta e)
        {
            var partes = new List<string>();
            if (e.Resultados.Count > 0)
                partes.Add(string.Join(" · ", e.Resultados.Select(r => $"{r.Metrica}: {Valor(r.Valor, r.Unidad)}")));
            else if (e.Niveles.Count > 0)
                partes.Add($"{e.Niveles.Count} nivel(es) de estrés");
            else
                partes.Add("— sin resultados");

            string? Cond(string clave) => e.Condiciones.FirstOrDefault(x => x.Clave == clave)?.Valor;
            var repro = new[] { ("SDK", Cond("SdkVersion")), ("TFM", Cond("TargetFramework")), ("Compilación", Cond("ConfiguracionCompilacion")), ("Entorno", Cond("Environment")) }
                .Where(x => !string.IsNullOrEmpty(x.Item2))
                .Select(x => $"{x.Item1}: {x.Item2}");
            var reproTexto = string.Join(" · ", repro);
            if (reproTexto.Length > 0) partes.Add(reproTexto);

            return string.Join("\n", partes);
        }

        // --------------------------------------------------------- conclusión

        private void Conclusion(IContainer c) => c.Column(col =>
        {
            col.Item().Text(ConclusionComparativa.Describir(_c.Metricas).Texto);
            col.Item().PaddingTop(2).Text("El cambio en «Tamaño de la aplicación» representa menor/mayor huella de recursos, no rendimiento temporal.")
                .FontSize(8).Italic().FontColor("#6b7280");

            if (!string.IsNullOrWhiteSpace(_o.ObservacionesFinales))
            {
                col.Item().PaddingTop(8).Text("Observaciones finales").SemiBold();
                col.Item().Text(_o.ObservacionesFinales!.Trim());
            }

            if (!string.IsNullOrWhiteSpace(_d.Observaciones))
            {
                col.Item().PaddingTop(8).Text("Observaciones de la evaluación").SemiBold();
                col.Item().Text(_d.Observaciones!.Trim());
            }
        });

        // --------------------------------------------------------- gráficos (small multiples)

        private void Graficos(IContainer c) => Seccion(c, "Gráficos comparativos", cuerpo => cuerpo.Column(col =>
        {
            col.Item().Text("Cada gráfico usa su propia escala según la unidad de la métrica; las alturas se comparan solo entre .NET 8 y .NET 10 del mismo gráfico.")
                .FontSize(8).Italic().FontColor("#6b7280");

            var resumen = _c.Metricas.Where(EsGraficable).ToList();
            if (resumen.Count > 0)
            {
                if (EsEstres) col.Item().PaddingTop(4).Text("Resumen (nivel máximo)").SemiBold();
                col.Item().PaddingTop(4).Element(cc => RejillaGraficos(cc, resumen.Select(m =>
                    new GraficoDatos(m.Nombre, m.Unidad, [new GrupoBarras(m.Nombre, m.PromedioNet8!.Value, m.PromedioNet10!.Value)], SharedScale: false)).ToList()));
            }

            if (_estres is { } e)
            {
                var porMetrica = GraficosNivel(e).ToList();
                if (porMetrica.Count > 0)
                {
                    col.Item().PaddingTop(8).Text("Por nivel de carga").SemiBold();
                    col.Item().PaddingTop(4).Element(cc => RejillaGraficos(cc, porMetrica));
                }
            }
        }));

        private IEnumerable<GraficoDatos> GraficosNivel(ComparacionEstres e)
        {
            foreach (var metrica in _d.MetricasSeleccionadas)
            {
                if (metrica.MetricaId == TamanoMetricaId) continue; // Tamaño nunca por nivel

                var grupos = new List<GrupoBarras>();
                foreach (var nivel in e.Niveles)
                {
                    var comp = nivel.Metricas.FirstOrDefault(x => x.MetricaId == metrica.MetricaId);
                    if (comp is not null && EsGraficable(comp))
                        grupos.Add(new GrupoBarras($"{nivel.UsuariosObjetivo} usuarios", comp.PromedioNet8!.Value, comp.PromedioNet10!.Value));
                }

                if (grupos.Count > 0)
                    yield return new GraficoDatos(metrica.Nombre, metrica.Unidad, grupos, SharedScale: true);
            }
        }

        private static void RejillaGraficos(IContainer c, IReadOnlyList<GraficoDatos> graficos) => c.Column(col =>
        {
            col.Spacing(8);
            // Dos gráficos por fila.
            for (var i = 0; i < graficos.Count; i += 2)
            {
                var izquierda = graficos[i];
                var derecha = i + 1 < graficos.Count ? graficos[i + 1] : null;
                col.Item().Row(row =>
                {
                    row.RelativeItem().Element(cc => UnGrafico(cc, izquierda));
                    row.ConstantItem(10);
                    if (derecha is not null) row.RelativeItem().Element(cc => UnGrafico(cc, derecha));
                    else row.RelativeItem().Text("");
                });
            }
        });

        private static void UnGrafico(IContainer c, GraficoDatos g) => c.Border(1).BorderColor("#e5e7eb").Padding(6).Column(col =>
        {
            const float plotH = 70f;
            col.Item().Text($"{g.Nombre} ({g.Unidad})").FontSize(9).SemiBold();

            var maxGlobal = g.Grupos.Count == 0 ? 0m : g.Grupos.Max(x => Math.Max(Math.Max(x.Net8, x.Net10), 0m));

            col.Item().PaddingTop(2).Row(row =>
            {
                foreach (var grupo in g.Grupos)
                {
                    var max = g.SharedScale ? maxGlobal : Math.Max(Math.Max(grupo.Net8, grupo.Net10), 0m);
                    row.RelativeItem().Column(gc =>
                    {
                        gc.Item().Row(barras =>
                        {
                            barras.RelativeItem().Element(cc => Barra(cc, grupo.Net8, max, plotH, ColorNet8));
                            barras.ConstantItem(4);
                            barras.RelativeItem().Element(cc => Barra(cc, grupo.Net10, max, plotH, ColorNet10));
                        });
                        gc.Item().AlignCenter().Text(grupo.Categoria).FontSize(7).FontColor("#6b7280");
                    });
                }
            });

            col.Item().PaddingTop(2).Row(leyenda =>
            {
                leyenda.AutoItem().Element(cc => cc.Width(8).Height(8).Background(ColorNet8));
                leyenda.AutoItem().PaddingLeft(2).PaddingRight(8).Text(".NET 8").FontSize(7);
                leyenda.AutoItem().Element(cc => cc.Width(8).Height(8).Background(ColorNet10));
                leyenda.AutoItem().PaddingLeft(2).Text(".NET 10").FontSize(7);
            });
        });

        private static void Barra(IContainer c, decimal valor, decimal max, float plotH, string color)
        {
            var altura = max <= 0m || valor <= 0m ? 0f : (float)((double)(valor / max) * plotH);
            c.Column(col =>
            {
                col.Item().AlignCenter().Text(N(valor)).FontSize(6);
                col.Item().Height(plotH).AlignBottom().Element(cc => cc.Height(altura).Background(color));
            });
        }

        // --------------------------------------------------------- formato (presentación, alineado a M-07)

        private static bool EsGraficable(ComparacionMetrica m) =>
            m.Interpretacion != InterpretacionComparacion.NoComparable && m.PromedioNet8 is not null && m.PromedioNet10 is not null;

        private static string N(decimal v) => v.ToString("0.####", CultureInfo.InvariantCulture);

        private static string Valor(decimal? v, string unidad) =>
            v is null ? "—" : string.IsNullOrEmpty(unidad) ? N(v.Value) : $"{N(v.Value)} {unidad}";

        private static string Diferencia(ComparacionMetrica m)
        {
            if (m.DiferenciaAbsoluta is not { } abs) return "—";
            var signo = abs > 0 ? "+" : string.Empty;
            var numero = $"{signo}{N(abs)}";
            return string.IsNullOrEmpty(m.Unidad) ? numero : $"{numero} {m.Unidad}";
        }

        private static string Porcentaje(decimal? p)
        {
            if (p is not { } pct) return "No calculable";
            var signo = pct > 0 ? "+" : string.Empty;
            return $"{signo}{pct.ToString("0.##", CultureInfo.InvariantCulture)} %";
        }

        private static string InterpretacionTexto(InterpretacionComparacion i) => i switch
        {
            InterpretacionComparacion.Mejora => "Mejora",
            InterpretacionComparacion.Empeora => "Empeora",
            InterpretacionComparacion.Igual => "Igual",
            _ => "No comparable"
        };

        private static string ParesTexto(ComparacionMetrica m)
        {
            if (m.MetricaId == TamanoMetricaId)
                return m.Interpretacion == InterpretacionComparacion.NoComparable
                    ? "valor constante no comparable"
                    : "1 comparación (valor constante)";
            return m.ParesUtilizados switch { 0 => "sin pares", 1 => "1 par", var n => $"{n} pares" };
        }

        private static string EstadoEjecucion(string estado) => estado switch
        {
            "Completada" => "Completada",
            "CompletadaConSaturacion" => "Completada con saturación",
            "Fallida" => "Fallida",
            "Cancelada" => "Cancelada",
            "EnProceso" => "En proceso",
            _ => estado
        };

        private static void EncabezadoCelda(IContainer c, string texto) =>
            c.Background("#eef2ff").Border(0.5f).BorderColor("#e5e7eb").Padding(3).Text(texto).FontSize(8).SemiBold();

        private static void Celda(IContainer c, string texto) =>
            c.Border(0.5f).BorderColor("#e5e7eb").Padding(3).Text(texto).FontSize(8);
    }

    private sealed record GrupoBarras(string Categoria, decimal Net8, decimal Net10);
    private sealed record GraficoDatos(string Nombre, string Unidad, IReadOnlyList<GrupoBarras> Grupos, bool SharedScale);
}
