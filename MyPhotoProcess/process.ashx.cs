using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace MyPhotoProcess
{
    /// <summary>
    /// process 的摘要说明
    /// </summary>
    public class process : IHttpHandler
    {
        public static decimal present;
        public static ArrayList processlist = new ArrayList();
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";

            string type = context.Request.Form["type"].ToString();
            if (type == "process")
            {
                string fileURL = context.Request.Form["txt"].ToString();
                int maxWidthHeight = int.Parse(context.Request.Form["value"].ToString());
                
                var result = ProcessFile(fileURL, maxWidthHeight);
                string json = JsonSerialize(result);
                context.Response.Write(json);
            }
            else
                if (type == "getPresent")
                {
                    context.Response.Write(present);
                }

        }

        /// <summary>
        /// 获取待处理照片列表
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static ArrayList GetPhotoList(string dir)
        {
            try
            {
                foreach (string d in Directory.GetFileSystemEntries(dir))
                {
                    try
                    {
                        if (File.Exists(d))
                        {
                            if (Path.GetExtension(d).ToLower() == ".jpg")
                            {
                                processlist.Add(d);
                            }
                            else
                            {
                                throw new Exception("Not a JPG File.");
                            }
                        }
                        else
                        {
                            string processChildFoder = System.Configuration.ConfigurationManager.AppSettings["processChildFoder"].ToString();
                            if (processChildFoder == "true")
                            {
                                DirectoryInfo d1 = new DirectoryInfo(d);
                                if (d1.GetFiles().Length != 0)
                                {
                                    GetPhotoList(d1.FullName);//递归
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return processlist;
        }

        public static ResultList ProcessFile(string dir, int maxWidthHeight)
        {
            ResultList result = new ResultList();
            try
            {
                //List<ResultInfo> result = new List<ResultInfo>();
                string isOverwrite = System.Configuration.ConfigurationManager.AppSettings["isOverwrite"].ToString();

                var list = GetPhotoList(dir);
                present = 0m;
                for (int i = 0; i < list.Count; i++)
                {
                    ResultInfo re = new ResultInfo();
                    try
                    {
                        present = (decimal)(i + 1) / list.Count;

                        re.FileName = Path.GetFileName(list[i].ToString());
                        re.FileURL = list[i].ToString();

                        string saveUrl = System.Configuration.ConfigurationManager.AppSettings["saveURL"].ToString();
                        if (isOverwrite == "true")
                        {
                            saveUrl = list[i].ToString().Replace(Path.GetFileName(list[i].ToString()), "");
                        }
                        saveUrl += ("/" + Path.GetFileNameWithoutExtension(list[i].ToString()) + "_process.jpg");

                        FileInfo fi = new FileInfo(re.FileURL);
                        if (fi.Attributes.ToString().IndexOf("ReadOnly") != -1)
                            fi.Attributes = FileAttributes.Normal;
                        // 获取照片的Exif信息  
                        var exif = GetImageProperties(re.FileURL);
                        MakeThumbnail(re.FileURL, saveUrl, maxWidthHeight, maxWidthHeight, "EQU", exif);
                        File.Delete(re.FileURL);
                        re.IsSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        re.IsSuccess = false;
                        re.ErrorMessage = ex.Message;
                    }
                    result.ProcessList.Add(re);
                }
                result.Base.ErrorCode=0;
            }
            catch (Exception ex)
            {
                result.Base.ErrorCode = 100;
                result.Base.ErrorMessage = ex.Message;
            }
            return result;
        }

        #region 图片缩放，多种指定方式生成图片
        /// <summary>  
        /// 图片缩放  
        /// </summary>  
        /// <param name="originalImagePath">原始图片路径，如：c:\\images\\1.gif</param>  
        /// <param name="thumbnailPath">生成缩略图图片路径，如：c:\\images\\2.gif</param>  
        /// <param name="width">宽</param>  
        /// <param name="height">高</param>  
        /// <param name="mode">EQU：指定最大高宽等比例缩放；HW：//指定高宽缩放（可能变形）；W:指定宽，高按比例；H:指定高，宽按比例；Cut：指定高宽裁减（不变形）</param>  
        public static void MakeThumbnail(string originalImagePath, string thumbnailPath, int width, int height, string mode, List<PropertyItem> exif)
        {
            System.Drawing.Image originalImage = System.Drawing.Image.FromFile(originalImagePath);

            int towidth = width;
            int toheight = height;

            int x = 0;
            int y = 0;
            int ow = originalImage.Width;
            int oh = originalImage.Height;

            if (mode == "EQU")//指定最大高宽，等比例缩放  
            {
                //if(height/oh>width/ow),如果高比例多，按照宽来缩放；如果宽的比例多，按照高来缩放  
                if (height * ow > width * oh)
                {
                    mode = "W";
                }
                else
                {
                    mode = "H";
                }
            }
            switch (mode)
            {
                case "HW"://指定高宽缩放（可能变形）  
                    break;
                case "W"://指定宽，高按比例  
                    toheight = originalImage.Height * width / originalImage.Width;
                    break;
                case "H"://指定高，宽按比例  
                    towidth = originalImage.Width * height / originalImage.Height;
                    break;
                case "Cut"://指定高宽裁减（不变形）  
                    if ((double)originalImage.Width / (double)originalImage.Height > (double)towidth / (double)toheight)
                    {
                        oh = originalImage.Height;
                        ow = originalImage.Height * towidth / toheight;
                        y = 0;
                        x = (originalImage.Width - ow) / 2;
                    }
                    else
                    {
                        ow = originalImage.Width;
                        oh = originalImage.Width * height / towidth;
                        x = 0;
                        y = (originalImage.Height - oh) / 2;
                    }
                    break;
                default:
                    break;
            }

            //新建一个bmp图片  
            System.Drawing.Image bitmap = new System.Drawing.Bitmap(towidth, toheight);

            //新建一个画板  
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap);

            //设置高质量插值法  
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;

            //设置高质量,低速度呈现平滑程度  
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            //清空画布并以透明背景色填充  
            g.Clear(System.Drawing.Color.Transparent);

            //在指定位置并且按指定大小绘制原图片的指定部分  
            g.DrawImage(originalImage, new System.Drawing.Rectangle(0, 0, towidth, toheight),
                new System.Drawing.Rectangle(x, y, ow, oh),
                System.Drawing.GraphicsUnit.Pixel);
            try
            {
                // 设置EXIF    
                foreach (PropertyItem pitem in exif)
                {
                    bitmap.SetPropertyItem(pitem);
                }

                //以jpg格式保存缩略图  
                bitmap.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Jpeg);

            }
            catch (System.Exception e)
            {
                throw e;
            }
            finally
            {
                originalImage.Dispose();
                bitmap.Dispose();
                g.Dispose();
            }
        }
        #endregion


        /// <summary>  
        /// 获取照片的Exif属性，存储成二进制list  
        /// </summary>  
        /// <param name="FileName"></param>  
        /// <returns></returns>  
        public static List<PropertyItem> GetImageProperties(string FileName)
        {
            if (!File.Exists(FileName)) return null;

            List<PropertyItem> rtn = new List<PropertyItem>();

            System.Drawing.Image img = null;

            try
            {
                img = System.Drawing.Image.FromFile(FileName);
                PropertyItem[] pt = img.PropertyItems;

                foreach (PropertyItem p in pt)
                {
                    rtn.Add(p);
                }
            }
            catch
            {
                rtn = null;
            }
            finally
            {
                if (img != null) img.Dispose();
            }

            return rtn;
        }


        /// <summary>    
        /// 将对象转换为 JSON 字符串。    
        /// </summary>    
        /// <param name="obj">要序列化的对象。</param>    
        /// <returns>序列化的 JSON 字符串。</returns>    
        public static string JsonSerialize(object obj)
        {
            JavaScriptSerializer jsSerializer = new JavaScriptSerializer();
            return jsSerializer.Serialize(obj);
        }


        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }

    public class ResultInfo
    {
        public string FileName { get; set; }
        public string FileURL { set; get; }
        public bool IsSuccess { get; set; }
        //public int ErrorCode { set; get; }
        public string ErrorMessage { get; set; }
    }

    public class BaseResult
    {
        public int ErrorCode { set; get; }
        public string ErrorMessage { get; set; }
    }

    public class ResultList
    {
        public BaseResult Base = new BaseResult();
        public List<ResultInfo> ProcessList = new List<ResultInfo>();
    }
}