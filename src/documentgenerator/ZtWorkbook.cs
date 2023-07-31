﻿using Syncfusion.XlsIO;
using Syncfusion.XlsIO.Implementation.Collections;
using ZeroTrustAssessment.DocumentGenerator.Graph;
using ZeroTrustAssessment.DocumentGenerator.Infrastructure;
using ZeroTrustAssessment.DocumentGenerator.Sheets;
using ZeroTrustAssessment.DocumentGenerator.ViewModels.Convert;

namespace ZeroTrustAssessment.DocumentGenerator;

public enum ZtSheets
{
    Home,
    WorkshopIdentity,
    WorkshopDevice,
    ConfigIdentity,
    ConfigDevice,
    AssessmentIdentity,
    AssessmentDevice
}

public class ZtWorkbook
{
    private readonly IWorkbook _workbook;
    private readonly GraphData? _graphData;

    public ZtWorkbook(IWorkbook workbook) : this(workbook, null) { }

    public ZtWorkbook(IWorkbook workbook, GraphData? graphData)
    {
        _workbook = workbook;
        _graphData = graphData;
    }

    public async Task GenerateDocumentAsync(IdPowerToys.PowerPointGenerator.Graph.GraphData pptxGraphData)
    {
        var sheetAssessmentDevice = new SheetAssessmentDevice(_workbook, ZtSheets.AssessmentDevice, _graphData);
        var deviceScore = sheetAssessmentDevice.Generate();

        var sheetAssessmentIdentity = new SheetAssessmentIdentity(_workbook, ZtSheets.AssessmentIdentity, _graphData);
        var identityScore = sheetAssessmentIdentity.Generate();

        var sheetConfigDevice = new SheetConfigDevice(_workbook, ZtSheets.ConfigDevice, _graphData);
        sheetConfigDevice.Generate();

        var sheetConfigIdentity = new SheetConfigIdentity(_workbook, ZtSheets.ConfigIdentity, _graphData);
        sheetConfigIdentity.Generate(pptxGraphData);

        var sheetHome = new SheetHome(_workbook, ZtSheets.Home, _graphData);
        sheetHome.Generate(identityScore, deviceScore);
        var sheet = GetWorksheet(_workbook, ZtSheets.Home);
        sheet.Activate();
    }

    /// <summary>
    /// Reads the roadmap info from the spreadsheet and returns a Roadmap object.
    /// </summary>
    /// <returns></returns>
    public async Task<Roadmap> GetRoadmapAsync()
    {
        var homeSheet = GetWorksheet(_workbook, ZtSheets.Home);
        var roadmap = new Roadmap();
        roadmap.TenantId = homeSheet.Range[ExcelConstant.HomeHeaderTenantIdLabel].Value;


        if (_workbook.Names is WorkbookNamesCollection names)
        {
            foreach (var name in names)
            {
                var key = name.Name;
                var roadmapList = key.StartsWith("RMI_") ? roadmap.Identity :
                    key.StartsWith("RMD_") ? roadmap.Device : null;

                if (roadmapList != null)
                {
                    var range = name.RefersToRange;
                    var status = name.RefersToRange.Value;
                    if (status != null)
                    {
                        var task = new RoadmapTask
                        {
                            Id = key,
                            Status = Labels.ConvertStatusLabelToString(status)
                        };

                        var parentRow = range.Row - 1;
                        var column = range.Column;

                        try
                        {
                            var titleRange = name.RefersToRange.Worksheet.Range[parentRow, column, parentRow, column];
                            var title = titleRange.Value;
                            var comment = titleRange.Comment.Text;
                            task.Title = title;
                            task.Description = comment;
                        }
                        catch { }
                        roadmapList.Add(task);
                    };
                }
            }
        }
        return roadmap;
    }


    public async Task ConvertToWorkbookAsync(Roadmap roadmap)
    {
        // roadmap.TenantId = homeSheet.Range[ExcelConstant.HomeHeaderTenantIdLabel].Value;
        var roadmapList = roadmap.Identity;
        roadmapList.AddRange(roadmap.Device);

        foreach (var item in roadmapList)
        {
            var key = item.Id;
            var status = Labels.ConvertStatusStringToLabel(item.Status);
            try
            {
                var range = _workbook.Names[key].RefersToRange;
                range.Value = status;
            }
            catch {}
        }
    }

    public static IWorksheet GetWorksheet(IWorkbook workbook, ZtSheets sheet)
    {
        return workbook.Worksheets[(int)sheet];
    }
}