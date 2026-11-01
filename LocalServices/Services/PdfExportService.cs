using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LocalServices.Models;

namespace LocalServices.Services;

public class PdfExportService : IPdfExportService
{
    static PdfExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    public byte[] GenerateFacilityPriceListPdf(Facility facility)
    {
        var categoryName = facility.Category?.Name ?? "Заклад";
        var cityName = string.IsNullOrWhiteSpace(facility.City) ? "Україна" : facility.City;
        var address = string.Join(", ", new[] { facility.City, facility.Street }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(address)) address = "Адреса уточнюється";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });

            void ComposeHeader(IContainer container)
            {
                container.Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(titleCol =>
                        {
                            titleCol.Item().Text("LocalServices").FontSize(12).Bold().FontColor(Colors.Blue.Medium);
                            titleCol.Item().Text($"Прайс-лист: {facility.Name}").FontSize(18).Bold().FontColor(Colors.Grey.Darken4);
                            titleCol.Item().Text($"{categoryName} • {cityName}").FontSize(11).FontColor(Colors.Grey.Medium);
                        });

                        row.ConstantItem(120).AlignRight().Column(metaCol =>
                        {
                            metaCol.Item().Text("Офіційний документ").FontSize(9).FontColor(Colors.Grey.Medium);
                            metaCol.Item().Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm")).FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                    });

                    col.Item().PaddingTop(10).Height(1).Background(Colors.Grey.Lighten2);
                });
            }

            void ComposeContent(IContainer container)
            {
                container.PaddingVertical(10).Column(col =>
                {
                    col.Item().Background(Colors.Grey.Lighten4).Padding(10).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Row(row =>
                    {
                        row.RelativeItem().Column(infoCol =>
                        {
                            infoCol.Item().Text(text =>
                            {
                                text.Span("Адреса: ").Bold();
                                text.Span(address);
                            });
                            infoCol.Item().Text(text =>
                            {
                                text.Span("Телефон: ").Bold();
                                text.Span(string.IsNullOrWhiteSpace(facility.Phone) ? "Не вказано" : facility.Phone);
                            });
                        });

                        row.RelativeItem().Column(infoCol =>
                        {
                            infoCol.Item().Text(text =>
                            {
                                text.Span("Години роботи: ").Bold();
                                text.Span(string.IsNullOrWhiteSpace(facility.WorkingHours) ? "Не вказано" : facility.WorkingHours);
                            });
                            infoCol.Item().Text(text =>
                            {
                                text.Span("Категорія: ").Bold();
                                text.Span(categoryName);
                            });
                        });
                    });

                    col.Item().PaddingTop(15).Text("Перелік послуг та актуальні ціни").FontSize(13).Bold().FontColor(Colors.Grey.Darken3);

                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignCenter().Text("#").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("Назва послуги").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("Ціна").Bold();
                        });

                        if (facility.Services != null && facility.Services.Any())
                        {
                            int index = 1;
                            foreach (var s in facility.Services.OrderBy(s => s.Name))
                            {
                                var bgColor = (index % 2 == 0) ? Colors.Grey.Lighten5 : Colors.White;

                                table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignCenter().Text(index.ToString());
                                table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(s.Name);
                                table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignRight().Text($"{s.Price:N2} грн").Bold().FontColor(Colors.Green.Darken2);

                                index++;
                            }
                        }
                        else
                        {
                            table.Cell().ColumnSpan(3).Padding(12).AlignCenter().Text("Послуги для цього закладу наразі відсутні в каталозі.").Italic().FontColor(Colors.Grey.Medium);
                        }
                    });

                    if (facility.Services != null && facility.Services.Any())
                    {
                        var totalServices = facility.Services.Count;
                        var minPrice = facility.Services.Min(s => s.Price);
                        var maxPrice = facility.Services.Max(s => s.Price);

                        col.Item().PaddingTop(10).AlignRight().Text(text =>
                        {
                            text.Span($"Всього послуг: {totalServices}  |  Діапазон цін: {minPrice:N2} – {maxPrice:N2} грн").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                    }
                });
            }

            void ComposeFooter(IContainer container)
            {
                container.Column(col =>
                {
                    col.Item().Height(0.5f).Background(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Згенеровано сервісом LocalServices (https://localservices-ecv8.onrender.com)")
                            .FontSize(8).FontColor(Colors.Grey.Medium);

                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.DefaultTextStyle(t => t.FontSize(8).FontColor(Colors.Grey.Medium));
                            x.Span("Сторінка ");
                            x.CurrentPageNumber();
                            x.Span(" з ");
                            x.TotalPages();
                        });
                    });
                });
            }
        });

        return document.GeneratePdf();
    }
}
