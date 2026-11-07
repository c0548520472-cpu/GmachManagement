using GmachManagement.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Security.Claims;
using System.Xml.Linq;
namespace GmachManagement.API.Controllers;
[ApiController,Route("api/gmachs/mine/import"),Authorize(Roles="GmachAdmin")]
public sealed class GmachImportController : ControllerBase {
 private readonly GmachService service;
 public GmachImportController(GmachService service)=>this.service=service;
 [HttpPost,RequestSizeLimit(2_000_000)]
 public async Task<IActionResult> Import(IFormFile file,CancellationToken ct){
  var userId=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
  var g=await service.GetMineAsync(userId,ct);
  if(g==null)return Forbid();
  if(file==null||file.Length==0||file.Length>2_000_000||!Path.GetExtension(file.FileName).Equals(".xlsx",StringComparison.OrdinalIgnoreCase))return BadRequest(new {message="יש להעלות קובץ Excel מסוג xlsx עד 2MB."});
  List<(string Name,string Description)> rows;
  try {using var stream=file.OpenReadStream();rows=ParseExcel(stream);}catch(Exception ex) when(ex is InvalidDataException or System.Xml.XmlException or ArgumentException){return BadRequest(new {message="קובץ Excel אינו תקין: "+ex.Message});}
  if(rows.Count==0)return BadRequest(new {message="אין שורות לייבוא."});
  var imported=await service.ImportItemsAsync(g.Id,rows,ct);
  return Ok(new {imported});
 }
 private static List<(string Name,string Description)> ParseExcel(Stream stream){
  using var zip=new ZipArchive(stream,ZipArchiveMode.Read);
  XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
  if(zip.Entries.Sum(e=>e.Length)>8_000_000)throw new InvalidDataException("קובץ Excel גדול מדי לאחר פתיחה.");
  var shared=new List<string>();
  var sharedEntry=zip.GetEntry("xl/sharedStrings.xml");
  if(sharedEntry!=null){using var reader=sharedEntry.Open();var doc=XDocument.Load(reader);shared=doc.Descendants(ns+"si").Select(x=>string.Concat(x.Descendants(ns+"t").Select(t=>t.Value))).ToList();}
  var sheet=zip.GetEntry("xl/worksheets/sheet1.xml")??throw new InvalidDataException("לא נמצא גיליון ראשון.");
  using var sheetReader=sheet.Open();var xml=XDocument.Load(sheetReader);
  var rows=xml.Descendants(ns+"sheetData").Elements(ns+"row").ToList();
  if(rows.Count>501)throw new InvalidDataException("ניתן לייבא עד 500 פריטים.");
  string Cell(XElement row,string col){var cell=row.Elements(ns+"c").FirstOrDefault(c=>new string(((string?)c.Attribute("r")??"").TakeWhile(char.IsLetter).ToArray()).Equals(col,StringComparison.OrdinalIgnoreCase));if(cell==null)return "";var v=cell.Element(ns+"v")?.Value??string.Concat(cell.Descendants(ns+"t").Select(t=>t.Value));if((string?)cell.Attribute("t")=="s" && int.TryParse(v,out int i)&&i>=0&&i<shared.Count)return shared[i];return v;}
  if(rows.Count<2) return new();
  var result=new List<(string,string)>();
  foreach(var row in rows.Skip(1)){
   var name=Cell(row,"A").Trim();var desc=Cell(row,"B").Trim();
   if(string.IsNullOrWhiteSpace(name)&&string.IsNullOrWhiteSpace(desc))continue;
   if(name.Length<2||name.Length>150||desc.Length>1000)throw new InvalidDataException("שם פריט חייב להכיל 2–150 תווים ותיאור עד 1000 תווים.");
   result.Add((name,desc));
  }
  return result;
 }
}
