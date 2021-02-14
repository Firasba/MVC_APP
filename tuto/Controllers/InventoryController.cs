using System;
using System.Collections.Generic;
using System.Data;
using System.Configuration;
using System.Data.OleDb;
using System.Data.Entity;
using System.Linq;

using System.Net;
using System.Web;
using System.Web.Mvc;
using tuto.DAL;
using tuto.Models;
using ClosedXML.Excel;
using tuto.CustomAttribute;
using System.IO;
using System.Text;
using System.Web.UI;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using System.Xml;
using ExcelDataReader;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace tuto.Controllers
{
    public class InventoryController : Controller
    {
        private InventoryRepository InventoryRepository;
        private InventoryContext db = new InventoryContext();
        
        public ActionResult Indexxx()
        {
            var Inventory = db.Inventories.ToList();

            return View(Inventory);
        }

        [HttpPost]
        public ActionResult ImportFromExcel(HttpPostedFileBase postedFile)
        {
            if (ModelState.IsValid)
            {
                if (postedFile != null && postedFile.ContentLength > (1024 * 1024 * 50))  // 50MB limit
                {
                    ModelState.AddModelError("postedFile", "Your file is to large. Maximum size allowed is 50MB !");
                }

                else
                {
                    string filePath = string.Empty;
                    string path = Server.MapPath("~/Uploads/");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    filePath = path + Path.GetFileName(postedFile.FileName);
                    string extension = Path.GetExtension(postedFile.FileName);
                    postedFile.SaveAs(filePath);

                    string conString = string.Empty;
                    switch (extension)
                    {
                        case ".xls": //For Excel 97-03.
                                     //  conString = ConfigurationManager.ConnectionStrings["Excel03ConString"].ConnectionString;
                            conString = "Provider=Microsoft.ACE.OLEDB.12.0; Data Source=" + filePath + ";Extended Properties='Excel 12.0;HDR=YES;IMEX=1;'";


                            break;
                        case ".xlsx": //For Excel 07 and above.
                                      // conString = ConfigurationManager.ConnectionStrings["Excel07ConString"].ConnectionString;
                            conString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath + ";Extended Properties='Excel 12.0;HDR=YES;IMEX=1;'"; 

                            break;
                    }

                    try
                    {
                        DataTable dt = new DataTable();
                        // conString = string.Format(conString, filePath);
                        //conString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath + ";cc

                        using (OleDbConnection connExcel = new OleDbConnection(conString))
                        {
                            using (OleDbCommand cmdExcel = new OleDbCommand())
                            {
                                using (OleDbDataAdapter odaExcel = new OleDbDataAdapter())
                                {
                                    cmdExcel.Connection = connExcel;

                                    //Get the name of First Sheet.
                                    connExcel.Open();
                                    DataTable dtExcelSchema;
                                    dtExcelSchema = connExcel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                                    string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                                    connExcel.Close();

                                    //Read Data from First Sheet.
                                    connExcel.Open();
                                    dt.Columns.Add("ID", typeof(int));


                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        dr["ID"] = GenerateID();
                                    }

                                    cmdExcel.CommandText = "SELECT [Board Name],[NEName],[Board Type],[SN(Bar Code)],[PN(BOM Code/Item)],[Manufacturer Data] From [" + sheetName + "]";
                                    odaExcel.SelectCommand = cmdExcel;
                                    odaExcel.Fill(dt);
                                    
                                    connExcel.Close();
       
                                }
                            }
                        }
                        dt.Columns["Board Name"].ColumnName = "BoardName";
                        dt.Columns["Board Type"].ColumnName = "BoardType";
                        dt.Columns["SN(Bar Code)"].ColumnName = "SN";
                        dt.Columns["PN(BOM Code/Item)"].ColumnName = "PN";
                         dt.Columns["Manufacturer Data"].ColumnName = "ManufacturerData";
                        conString = ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString;
                        using (SqlConnection con = new SqlConnection(conString))
                        {
                            using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(con))
                            {
                                //Set the database table name.
                                sqlBulkCopy.DestinationTableName = "Inventory";
                                con.Open();
                                sqlBulkCopy.WriteToServer(dt);
                                con.Close();
                                return Json("File uploaded successfully");
                            }
                        }
                    }

                    //catch (Exception ex)
                    //{
                    //    throw ex;
                    //}
                    catch (Exception e)
                    {
                        return Json("error" + e.Message);
                    }
                    //return RedirectToAction("Index");
                }
            }
            //return View(postedFile);
            return Json("no files were selected !");
        }

        public ActionResult Search(string  id)
        {
            var inventories = from m in db.Inventories
                              select m;

            if (!String.IsNullOrEmpty(id))
            {
                inventories = inventories.Where(s => s.BoardName.Contains(id));
            }

            return View(inventories.ToList());

        }

        public ActionResult Search2(string id1, string id2, string id3, string id4, string id5)
        {
            var inventories = from m in db.Inventories
                              select m;

            if (!String.IsNullOrEmpty(id1))
            {
                inventories = inventories.Where(s => s.BoardName.Contains(id1)); 
            }
            if (!String.IsNullOrEmpty(id2))
            {
                inventories = inventories.Where(s => s.NEName.Contains(id2));
            }
            if (!String.IsNullOrEmpty(id3))
            {
                inventories = inventories.Where(s => s.BoardType.Contains(id3));
            }
            if (!String.IsNullOrEmpty(id4))
            {
                inventories = inventories.Where(s => s.SN.Contains(id4));
            }
            if (!String.IsNullOrEmpty(id5))
            {
                inventories = inventories.Where(s => s.PN.Contains(id5));
            }
            return View(inventories.ToList());

        }
        [HttpPost]
        [AllowMultipleButton(Name = "action", Argument = "ExportToExcel")]
        public ActionResult ExportToExcel()
        {
            
            DataTable dtInventory = GetAllInventoriesDetail();
            /*DataView view = new DataView(dtInventory);
            view.Sort = "BoardName";
            DataTable dtStatistics = view.ToTable(true,"BoardName");*/
            var res = (from x in dtInventory.AsEnumerable()
                       group x by (string)x["BoardName"] into y
                       select new { Key = y.Key, Count = y.Count() }).ToArray();
            DataTable stats = new DataTable();
            stats.Columns.Add("BoardName", typeof(String));
            stats.Columns.Add("Quantity", typeof(int));

            var res2 = (from x in dtInventory.AsEnumerable()
                       group x by (string)x["BoardType"] into y
                       select new { Key = y.Key, Count = y.Count() }).ToArray();
            DataTable stats2 = new DataTable();
            stats2.Columns.Add("BoardType", typeof(String));
            stats2.Columns.Add("Quantity", typeof(int));

            var res3  = (from x in dtInventory.AsEnumerable()
                        group x by (string)x["ManufacturerData"] into y
                        select new { Key = y.Key, Count = y.Count() }).ToArray();
            DataTable stats3 = new DataTable();
            stats3.Columns.Add("ManufacturerData", typeof(String));
            stats3.Columns.Add("Quantity", typeof(int));

            foreach (var pair in res)
            {
               // Console.WriteLine("KEY, Count: {0} ,{1}");
                /* */
                string k = pair.Key;
                int c = pair.Count;

                stats.Rows.Add(k, c);
            }
            foreach (var pair in res2)
            {
                // Console.WriteLine("KEY, Count: {0} ,{1}");
                /* */
                string k = pair.Key;
                int c = pair.Count;

                stats2.Rows.Add(k, c);
            }
            foreach (var pair in res3)
            {
                // Console.WriteLine("KEY, Count: {0} ,{1}");
                /* */
                string k = pair.Key;
                int c = pair.Count;

                stats3.Rows.Add(k, c);
            }

            using (XLWorkbook woekBook = new XLWorkbook())
            {
                woekBook.Worksheets.Add(dtInventory,"details");
               woekBook.Worksheets.Add(stats, "statistics based on Board Name");
                woekBook.Worksheets.Add(stats2, "statistics based on Board Type");
                woekBook.Worksheets.Add(stats3, "stats based on Manufacturer");
                using (MemoryStream stream = new MemoryStream())
                {
                    woekBook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InventoryDetails.xlsx");
                }
               
            }
        }
        [HttpPost]
        [AllowMultipleButton(Name = "action", Argument = "ExportToCsv")]
        public ActionResult ExportToCsv(int? pageNumber)
        {
            DataTable dtInventory = GetInventoriesDetail(pageNumber);

            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = dtInventory.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dtInventory.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field =>
                  string.Concat("\"", field.ToString().Replace("\"", "\"\""), "\""));
                sb.AppendLine(string.Join(",", fields));
            }
            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment;filename=InventoryDetails.csv");
            Response.Charset = "";
            Response.ContentType = "application/text";
            Response.Output.Write(sb);
            Response.Flush();
            Response.End();

            return View("Index");
        }
        [HttpPost]
        [AllowMultipleButton(Name = "action", Argument = "ExportToPdf")]
        public ActionResult ExportToPdf(int? pageNumber)
        {
            DataTable dtInventory = GetInventoriesDetail(pageNumber);

            if (dtInventory.Rows.Count > 0)
            {
                int pdfRowIndex = 1;

                string filename = "InventoryDetails-" + DateTime.Now.ToString("dd-MM-yyyy hh_mm_s_tt");
                string filepath = Server.MapPath("\\") + "" + filename + ".pdf";
                Document document = new Document(PageSize.A4, 5f, 5f, 10f, 10f);
                FileStream fs = new FileStream(filepath, FileMode.Create);
                PdfWriter writer = PdfWriter.GetInstance(document, fs);
                document.Open();

                Font font1 = FontFactory.GetFont(FontFactory.COURIER_BOLD, 10);
                Font font2 = FontFactory.GetFont(FontFactory.COURIER, 8);

                float[] columnDefinitionSize = { 2F, 5F, 2F, 5F };
                PdfPTable table;
                PdfPCell cell;

                table = new PdfPTable(columnDefinitionSize)
                {
                    WidthPercentage = 100
                };

                cell = new PdfPCell
                {
                    BackgroundColor = new BaseColor(0xC0, 0xC0, 0xC0)
                };

                table.AddCell(new Phrase("ID", font1));
                table.AddCell(new Phrase("BoardName", font1));
                table.AddCell(new Phrase("NEName", font1));
                table.AddCell(new Phrase("BoardType", font1));
                table.AddCell(new Phrase("SN", font1));
                table.AddCell(new Phrase("PN", font1));
                table.HeaderRows = 1;

                foreach (DataRow data in dtInventory.Rows)
                {
                    table.AddCell(new Phrase(data["ID"].ToString(), font2));
                    table.AddCell(new Phrase(data["BoardName"].ToString(), font2));
                    table.AddCell(new Phrase(data["NEName"].ToString(), font2));
                    table.AddCell(new Phrase(data["BoardType"].ToString(), font2));
                    table.AddCell(new Phrase(data["SN"].ToString(), font2));
                    table.AddCell(new Phrase(data["PN"].ToString(), font2));
                    pdfRowIndex++;
                }

                document.Add(table);
                document.Close();
                document.CloseDocument();
                document.Dispose();
                writer.Close();
                writer.Dispose();
                fs.Close();
                fs.Dispose();

                FileStream sourceFile = new FileStream(filepath, FileMode.Open);
                float fileSize = 0;
                fileSize = sourceFile.Length;
                byte[] getContent = new byte[Convert.ToInt32(Math.Truncate(fileSize))];
                sourceFile.Read(getContent, 0, Convert.ToInt32(sourceFile.Length));
                sourceFile.Close();
                Response.ClearContent();
                Response.ClearHeaders();
                Response.Buffer = true;
                Response.ContentType = "application/pdf";
                Response.AddHeader("Content-Length", getContent.Length.ToString());
                Response.AddHeader("Content-Disposition", "attachment; filename=" + filename + ".pdf;");
                Response.BinaryWrite(getContent);
                Response.Flush();
                Response.End();
            }
            return View("Index");
        }

        
        [HttpPost]
        [AllowMultipleButton(Name = "action", Argument = "ExportToWord")]
        public ActionResult ExportToWord(int? pageNumber)
        {
            DataTable dtInventory = GetInventoriesDetail(pageNumber);

            if (dtInventory.Rows.Count > 0)
            {
                StringBuilder sbDocumentBody = new StringBuilder();

                sbDocumentBody.Append("<table width=\"100%\" style=\"background-color:#ffffff;\">");
                //  
                if (dtInventory.Rows.Count > 0)
                {
                    sbDocumentBody.Append("<tr><td>");
                    sbDocumentBody.Append("<table width=\"600\" cellpadding=0 cellspacing=0 style=\"border: 1px solid gray;\">");

                    // Add Column Headers dynamically from datatable  
                    sbDocumentBody.Append("<tr>");
                    for (int i = 0; i < dtInventory.Columns.Count; i++)
                    {
                        sbDocumentBody.Append("<td class=\"Header\" width=\"120\" style=\"border: 1px solid gray; text-align:center; font-family:Verdana; font-size:12px; font-weight:bold;\">" + dtInventory.Columns[i].ToString().Replace(".", "<br>") + "</td>");
                    }
                    sbDocumentBody.Append("</tr>");

                    // Add Data Rows dynamically from datatable  
                    for (int i = 0; i < dtInventory.Rows.Count; i++)
                    {
                        sbDocumentBody.Append("<tr>");
                        for (int j = 0; j < dtInventory.Columns.Count; j++)
                        {
                            sbDocumentBody.Append("<td class=\"Content\"style=\"border: 1px solid gray;\">" + dtInventory.Rows[i][j].ToString() + "</td>");
                        }
                        sbDocumentBody.Append("</tr>");
                    }
                    sbDocumentBody.Append("</table>");
                    sbDocumentBody.Append("</td></tr></table>");
                }
                Response.Clear();
                Response.Buffer = true;
                Response.AppendHeader("Content-Type", "application/msword");
                Response.AppendHeader("Content-disposition", "attachment; filename=ProductDetails.doc");
                Response.Write(sbDocumentBody.ToString());
                Response.End();
            }
            return View("Index");
        }
        [HttpPost]
        [AllowMultipleButton(Name = "action", Argument = "ExportToExcel2")]
        public ActionResult ExportToExcel2(string id1,string id2,string id3,string id4,string id5)
        {
            DataTable dtInventory = GetInventoriesDetail2(id1,id2,id3,id4,id5);

            using (XLWorkbook woekBook = new XLWorkbook())
            {
                woekBook.Worksheets.Add(dtInventory);
                using (MemoryStream stream = new MemoryStream())
                {
                    woekBook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InventoryDetails.xlsx");
                }
            }
        }
        [HttpPost]
        [AllowMultipleButton(Name = "action", Argument = "ExportToCsv2")]
        public ActionResult ExportToCsv2(string id1, string id2, string id3, string id4, string id5)
        {
            DataTable dtInventory = GetInventoriesDetail2(id1, id2, id3, id4, id5);

            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = dtInventory.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dtInventory.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field =>
                  string.Concat("\"", field.ToString().Replace("\"", "\"\""), "\""));
                sb.AppendLine(string.Join(",", fields));
            }
            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment;filename=InventoryDetails.csv");
            Response.Charset = "";
            Response.ContentType = "application/text";
            Response.Output.Write(sb);
            Response.Flush();
            Response.End();

            return View("Search2");
        }
        [HttpPost]
        [AllowMultipleButton(Name = "action", Argument = "ExportToPdf2")]
        public ActionResult ExportToPdf2(string id1, string id2, string id3, string id4, string id5)
        {
            DataTable dtInventory = GetInventoriesDetail2(id1,id2,id3,id4,id5);

            if (dtInventory.Rows.Count > 0)
            {
                int pdfRowIndex = 1;

                string filename = "InventoryDetails-" + DateTime.Now.ToString("dd-MM-yyyy hh_mm_s_tt");
                string filepath = Server.MapPath("\\") + "" + filename + ".pdf";
                Document document = new Document(PageSize.A4, 5f, 5f, 10f, 10f);
                FileStream fs = new FileStream(filepath, FileMode.Create);
                PdfWriter writer = PdfWriter.GetInstance(document, fs);
                document.Open();

                Font font1 = FontFactory.GetFont(FontFactory.COURIER_BOLD, 10);
                Font font2 = FontFactory.GetFont(FontFactory.COURIER, 8);

                float[] columnDefinitionSize = { 2F, 5F, 2F, 5F };
                PdfPTable table;
                PdfPCell cell;

                table = new PdfPTable(columnDefinitionSize)
                {
                    WidthPercentage = 100
                };

                cell = new PdfPCell
                {
                    BackgroundColor = new BaseColor(0xC0, 0xC0, 0xC0)
                };

                table.AddCell(new Phrase("ID", font1));
                table.AddCell(new Phrase("BoardName", font1));
                table.AddCell(new Phrase("NEName", font1));
                table.AddCell(new Phrase("BoardType", font1));
                table.AddCell(new Phrase("SN", font1));
                table.AddCell(new Phrase("PN", font1));
                table.HeaderRows = 1;

                foreach (DataRow data in dtInventory.Rows)
                {
                    table.AddCell(new Phrase(data["ID"].ToString(), font2));
                    table.AddCell(new Phrase(data["BoardName"].ToString(), font2));
                    table.AddCell(new Phrase(data["NEName"].ToString(), font2));
                    table.AddCell(new Phrase(data["BoardType"].ToString(), font2));
                    table.AddCell(new Phrase(data["SN"].ToString(), font2));
                    table.AddCell(new Phrase(data["PN"].ToString(), font2));
                    pdfRowIndex++;
                }

                document.Add(table);
                document.Close();
                document.CloseDocument();
                document.Dispose();
                writer.Close();
                writer.Dispose();
                fs.Close();
                fs.Dispose();

                FileStream sourceFile = new FileStream(filepath, FileMode.Open);
                float fileSize = 0;
                fileSize = sourceFile.Length;
                byte[] getContent = new byte[Convert.ToInt32(Math.Truncate(fileSize))];
                sourceFile.Read(getContent, 0, Convert.ToInt32(sourceFile.Length));
                sourceFile.Close();
                Response.ClearContent();
                Response.ClearHeaders();
                Response.Buffer = true;
                Response.ContentType = "application/pdf";
                Response.AddHeader("Content-Length", getContent.Length.ToString());
                Response.AddHeader("Content-Disposition", "attachment; filename=" + filename + ".pdf;");
                Response.BinaryWrite(getContent);
                Response.Flush();
                Response.End();
            }
            return View("Search2");
        }
        [HttpPost]
        [AllowMultipleButton(Name = "action", Argument = "ExportToWord2")]
        public ActionResult ExportToWord2(string id1, string id2, string id3, string id4, string id5)
        {
            DataTable dtInventory = GetInventoriesDetail2(id1,id2,id3,id4,id5);

            if (dtInventory.Rows.Count > 0)
            {
                StringBuilder sbDocumentBody = new StringBuilder();

                sbDocumentBody.Append("<table width=\"100%\" style=\"background-color:#ffffff;\">");
                //  
                if (dtInventory.Rows.Count > 0)
                {
                    sbDocumentBody.Append("<tr><td>");
                    sbDocumentBody.Append("<table width=\"600\" cellpadding=0 cellspacing=0 style=\"border: 1px solid gray;\">");

                    // Add Column Headers dynamically from datatable  
                    sbDocumentBody.Append("<tr>");
                    for (int i = 0; i < dtInventory.Columns.Count; i++)
                    {
                        sbDocumentBody.Append("<td class=\"Header\" width=\"120\" style=\"border: 1px solid gray; text-align:center; font-family:Verdana; font-size:12px; font-weight:bold;\">" + dtInventory.Columns[i].ToString().Replace(".", "<br>") + "</td>");
                    }
                    sbDocumentBody.Append("</tr>");

                    // Add Data Rows dynamically from datatable  
                    for (int i = 0; i < dtInventory.Rows.Count; i++)
                    {
                        sbDocumentBody.Append("<tr>");
                        for (int j = 0; j < dtInventory.Columns.Count; j++)
                        {
                            sbDocumentBody.Append("<td class=\"Content\"style=\"border: 1px solid gray;\">" + dtInventory.Rows[i][j].ToString() + "</td>");
                        }
                        sbDocumentBody.Append("</tr>");
                    }
                    sbDocumentBody.Append("</table>");
                    sbDocumentBody.Append("</td></tr></table>");
                }
                Response.Clear();
                Response.Buffer = true;
                Response.AppendHeader("Content-Type", "application/msword");
                Response.AppendHeader("Content-disposition", "attachment; filename=ProductDetails.doc");
                Response.Write(sbDocumentBody.ToString());
                Response.End();
            }
            return View("Search2");
        }

        private int GenerateID()
        { int count = new int();
            string conString = ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString;
            SqlConnection con = new SqlConnection(conString);
               // SqlConnection con = new SqlConnection("Data Source=(local);" +
                 //        "Initial Catalog=tuto1;Integrated Security=SSPI");
            con.Open();
            SqlCommand cmd = new SqlCommand("Select Max(ID) from Inventory", con);
            SqlDataReader dr = cmd.ExecuteReader();
            string newId = string.Format("050-{0}-0001", DateTime.Now.Year);
            if (dr.HasRows)
            {
                string prefix = string.Format("050-{0}", DateTime.Now.Year);
                while (dr.Read())
                {

                    string maxId = dr[0].ToString();
                    if (!string.IsNullOrWhiteSpace(maxId) && maxId.StartsWith(prefix))
                    {
                        count = Convert.ToInt32(maxId.Split('-')[2]);
                        
                    }
                }
            }
            return (count);
            
            con.Close();
        }
        private List<Inventory> GetDataFromCSVFile(Stream stream)
        {
            var invList = new List<Inventory>();
            try
            {
                using (var reader = ExcelReaderFactory.CreateCsvReader(stream))
                {
                    var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration
                        {
                            UseHeaderRow = true // To set First Row As Column Names  
                        }
                    });

                    if (dataSet.Tables.Count > 0)
                    {
                        var dataTable = dataSet.Tables[0];
                        foreach (DataRow objDataRow in dataTable.Rows)
                        {
                            int ID = GenerateID();
                            if (objDataRow.ItemArray.All(x => string.IsNullOrEmpty(x?.ToString()))) continue;
                            invList.Add(new Inventory()
                            {
                                //ID = Convert.ToInt32(objDataRow["ID"].ToString()),
                                ID =ID,
                                BoardName = objDataRow["BoardName"].ToString(),
                                NEName = objDataRow["NEName"].ToString(),
                                BoardType = objDataRow["BoardType"].ToString(),
                                SN = objDataRow["SN"].ToString(),
                                PN = objDataRow["PN"].ToString(),
                                ManufacturerData = objDataRow["ManufacturerData"].ToString(),
                            }) ;
                        }
                    }

                }
            }
            catch (Exception)
            {
                throw;
            }

            return invList;
        }
        [HttpPost]
        public async Task<ActionResult> ImportFile(HttpPostedFileBase  importFile)
        {
            if (importFile == null) return Json(new { Status = 0, Message = "No File Selected" });

             try
              {
                  var fileData = GetDataFromCSVFile(importFile.InputStream);

                  var dtInventory = fileData.ToDataTable();
                  var tblInventoryParameter = new SqlParameter("tblInventoryTableType", SqlDbType.Structured)
                  {
                      TypeName = "dbo.tblTypeInventory",
                      Value = dtInventory
                  };
                  await db.Database.ExecuteSqlCommandAsync("EXEC spBulkImportInventory @tblInventoryTableType", tblInventoryParameter);
                  return Json(new { Status = 1, Message = "File Imported Successfully " });

              }
              catch (Exception ex)
              {
                  return Json(new { Status = 0, Message = ex.Message });
              }
            /*var fileData = GetDataFromCSVFile(importFile.InputStream);

            var dtInventory = fileData.ToDataTable();


            using (connectionObject)
            {
                SqlCommand cmd = new SqlCommand("dbo.InsertMyDataTable", connectionObject);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlParameter tvparam = cmd.Parameters.AddWithValue("@dt", tvp);
                tvparam.SqlDbType = SqlDbType.Structured;
                cmd.ExecuteNonQuery();
            }*/

        }
        public ActionResult Indexx()
        {
            return View();
        }
        public ActionResult Index()
        {
            return View(db.Inventories.ToList());
        }
        [HttpGet]
        public ActionResult InventoryList(int? pageNumber)
        {
            InventoryRepository = new InventoryRepository();
            var model = InventoryRepository.GetInventories(pageNumber);
            return PartialView("~/Views/Inventory/InventoryList.cshtml", model);
        }
        [HttpGet]
        public ActionResult InventoryList2(string id1, string id2, string id3, string id4, string id5)
        {
            InventoryRepository = new InventoryRepository();
            
            
            var inventories = from m in db.Inventories
                              select m;

            if (!String.IsNullOrEmpty(id1))
            {
                inventories = inventories.Where(s => s.BoardName.Contains(id1));
            }
            if (!String.IsNullOrEmpty(id2))
            {
                inventories = inventories.Where(s => s.NEName.Contains(id2));
            }
            if (!String.IsNullOrEmpty(id3))
            {
                inventories = inventories.Where(s => s.BoardType.Contains(id3));
            }
            if (!String.IsNullOrEmpty(id4))
            {
                inventories = inventories.Where(s => s.SN.Contains(id4));
            }
            if (!String.IsNullOrEmpty(id5))
            {
                inventories = inventories.Where(s => s.PN.Contains(id5));
            }
            var model = inventories;
            return PartialView("~/Views/Inventory/InventoryList2.cshtml", model);
        }
        private DataTable GetInventoriesDetail2(string id1, string id2,string id3,string id4,string id5)
        {

            InventoryRepository = new InventoryRepository();
            var inventories = from m in db.Inventories
                              select m;

            if (!String.IsNullOrEmpty(id1))
            {
                inventories = inventories.Where(s => s.BoardName.Contains(id1));
            }
            if (!String.IsNullOrEmpty(id2))
            {
                inventories = inventories.Where(s => s.NEName.Contains(id2));
            }
            if (!String.IsNullOrEmpty(id3))
            {
                inventories = inventories.Where(s => s.BoardType.Contains(id3));
            }
            if (!String.IsNullOrEmpty(id4))
            {
                inventories = inventories.Where(s => s.SN.Contains(id4));
            }
            if (!String.IsNullOrEmpty(id5))
            {
                inventories = inventories.Where(s => s.PN.Contains(id5));
            }

            DataTable dtInventory = new DataTable("InventoryDetails");
            dtInventory.Columns.AddRange(new DataColumn[7] { new DataColumn("ID"),
                                            new DataColumn("BoardName"),
                                            new DataColumn("NEName"),
                                            new DataColumn("BoardType"),
                                            new DataColumn("SN"),
                                            new DataColumn("PN"),new DataColumn("ManufacturerData"),});
            foreach (var inventory in inventories)
            {
                dtInventory.Rows.Add(inventory.ID, inventory.BoardName, inventory.NEName, inventory.BoardType, inventory.SN, inventory.PN, inventory.ManufacturerData);
            }

            return dtInventory;
        }

        private DataTable GetAllInventoriesDetail()
        {
            InventoryRepository = new InventoryRepository();
            var inventories = from m in db.Inventories
                              select m;

            DataTable dtInventory = new DataTable("InventoryDetails");
            dtInventory.Columns.AddRange(new DataColumn[7] { new DataColumn("ID"),
                                            new DataColumn("BoardName"),
                                            new DataColumn("NEName"),
                                            new DataColumn("BoardType"),
                                            new DataColumn("SN"),
                                            new DataColumn("PN"),new DataColumn("ManufacturerData")});
            foreach (var inventory in inventories)
            {
                dtInventory.Rows.Add(inventory.ID, inventory.BoardName, inventory.NEName, inventory.BoardType, inventory.SN, inventory.PN, inventory.ManufacturerData);
            }

            return dtInventory;
        }


        private DataTable GetInventoriesDetail(int? pageNumber)
        {
            InventoryRepository = new InventoryRepository();
            var inventories = InventoryRepository.GetInventories(pageNumber);

            DataTable dtInventory = new DataTable("InventoryDetails");
            dtInventory.Columns.AddRange(new DataColumn[7] { new DataColumn("ID"),
                                            new DataColumn("BoardName"),
                                            new DataColumn("NEName"),
                                            new DataColumn("BoardType"),
                                            new DataColumn("SN"),
                                            new DataColumn("PN"),new DataColumn("ManufacturerData")});
            foreach (var inventory in inventories)
    {
                dtInventory.Rows.Add(inventory.ID, inventory.BoardName, inventory.NEName, inventory.BoardType, inventory.SN, inventory.PN, inventory.ManufacturerData);
            }

            return dtInventory;
        }

        // GET: Inventory/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Inventory inventory = db.Inventories.Find(id);
            if (inventory == null)
            {
                return HttpNotFound();
            }
            return View(inventory);
        }
        [HttpPost]
        [AllowMultipleButton(Name = "action", Argument = "DeleteAllR")]
        public ActionResult DeleteAllR (int? pageNumber)
        {
            db.Inventories.RemoveRange(db.Inventories);
            db.SaveChanges();
            return View();

        }
        // GET: Inventory/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Inventory/Create
        // Afin de déjouer les attaques par survalidation, activez les propriétés spécifiques auxquelles vous voulez établir une liaison. Pour 
        // plus de détails, consultez https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,BoardName,NEName,BoardType,SN,PN,ManufacturerData")] Inventory inventory)
        {
            if (ModelState.IsValid)
            {
                db.Inventories.Add(inventory);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(inventory);
        }

        // GET: Inventory/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Inventory inventory = db.Inventories.Find(id);
            if (inventory == null)
            {
                return HttpNotFound();
            }
            return View(inventory);
        }

        // POST: Inventory/Edit/5
        // Afin de déjouer les attaques par survalidation, activez les propriétés spécifiques auxquelles vous voulez établir une liaison. Pour 
        // plus de détails, consultez https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,BoardName,NEName,BoardType,SN,PN,ManufacturerData")] Inventory inventory)
        {
            if (ModelState.IsValid)
            {
                db.Entry(inventory).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(inventory);
        }

        // GET: Inventory/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Inventory inventory = db.Inventories.Find(id);
            if (inventory == null)
            {
                return HttpNotFound();
            }
            return View(inventory);
        }

        // POST: Inventory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Inventory inventory = db.Inventories.Find(id);
            db.Inventories.Remove(inventory);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
