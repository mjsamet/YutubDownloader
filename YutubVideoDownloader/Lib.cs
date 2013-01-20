using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Web;
using System.Net;
using System.Collections.Specialized;

namespace YutubVideoDownloader
{
    public delegate void dlgDownlading(int DownloadedBytes, int TotalBytes);

    public struct YutubFileFormat
    {
        void SetExtDim()
        {
            switch (_videoCode)
            {
                case 5:
                    VideoExt = "FLV";
                    VideoRess = "240p";
                    break;
                case 6:
                    VideoExt = "FLV";
                    VideoRess = "270p";
                    break;
                case 34:
                    VideoExt = "FLV";
                    VideoRess = "360p";
                    break;
                case 35:
                    VideoExt = "FLV";
                    VideoRess = "480p";
                    break;
                case 13:
                    VideoExt = "3GP";
                    VideoRess = "N/A";
                    break;
                case 17:
                    VideoExt = "3GP";
                    VideoRess = "144p";
                    break;
                case 36:
                    VideoExt = "3GP";
                    VideoRess = "240p";
                    break;
                case 18:
                    VideoExt = "MP4";
                    VideoRess = "360p";
                    break;
                case 22:
                    VideoExt = "MP4";
                    VideoRess = "720p";
                    break;
                case 37:
                    VideoExt = "MP4";
                    VideoRess = "1080p";
                    break;
                case 38:
                    VideoExt = "MP4";
                    VideoRess = "3072p";
                    break;
                case 82:
                    VideoExt = "MP4";
                    VideoRess = "360p";
                    break;
                case 83:
                    VideoExt = "MP4";
                    VideoRess = "240p";
                    break;
                case 84:
                    VideoExt = "MP4";
                    VideoRess = "720p";
                    break;
                case 85:
                    VideoExt = "MP4";
                    VideoRess = "520p";
                    break;
                case 43:
                    VideoExt = "WebM";
                    VideoRess = "360p";
                    break;
                case 44:
                    VideoExt = "WebM";
                    VideoRess = "480p";
                    break;
                case 45:
                    VideoExt = "WebM";
                    VideoRess = "720p";
                    break;
                case 46:
                    VideoExt = "WebM";
                    VideoRess = "1080p";
                    break;
                case 100:
                    VideoExt = "WebM";
                    VideoRess = "360p";
                    break;
                case 101:
                    VideoExt = "WebM";
                    VideoRess = "360p";
                    break;
                case 102:
                    VideoExt = "WebM";
                    VideoRess = "720p";
                    break;
            }
        }

        int _videoCode;
        public int VideoCode
        {
            get { return _videoCode; }
            set { _videoCode = value; this.SetExtDim(); }
        }

        public string VideoExt { get; set; }
        public string VideoRess { get; set; }
        public string DownloadURL { get; set; }
        public string VideoQu { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1} - {2}", VideoRess, VideoExt, VideoQu);
            //return base.ToString();
        }
    }

    public class YutubFile
    {
        #region Özellikler
        string _videoURL;
        public string VideoURL
        {
            get { return _videoURL; }
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                if (!Uri.IsWellFormedUriString(value, UriKind.RelativeOrAbsolute)) throw new Exception("Url error");
                _videoURL = value;
                FormatList.Clear();
                GetVideoInfo();
            }
        }

        string _videoTitle;
        public string VideoTitle
        {
            get { return _videoTitle; }
        }

        string _length;
        public string Length
        {
            get { return _length; }
            set { _length = value; }
        }

        public string ThumbUrl { get; set; }


        List<YutubFileFormat> _formatList;
        public List<YutubFileFormat> FormatList
        {
            get
            {
                if (_formatList == null) _formatList = new List<YutubFileFormat>();
                return _formatList;
            }
            set { _formatList = value; }
        }
        #endregion

        public YutubFile()
        {

        }

        public YutubFile(string URL)
        {
            this.VideoURL = URL;
        }

        void GetVideoInfo()
        {
            if (string.IsNullOrEmpty(_videoURL)) return;
            string videoId = HttpUtility.ParseQueryString(this._videoURL)[0];
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(string.Format("http://www.youtube.com/get_video_info?&video_id={0}{1}&ps=default&eurl=&gl=US&hl=en", videoId, "&el=vevo"));
            req.Method = "GET";
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            string videoinfo = "";
            using (StreamReader sr = new StreamReader(res.GetResponseStream()))
            {
                videoinfo = sr.ReadToEnd();
                sr.Close();
            }
            res.Close();
            NameValueCollection list = HttpUtility.ParseQueryString(videoinfo);
            this._videoTitle = list["title"];
            this.ThumbUrl = list["thumbnail_url"];
            int seconds = int.Parse(list["length_seconds"]);
            this._length = string.Format("{0}:{1}", (int)(seconds / 60), seconds % 60);
            string[] fmt_map = list["url_encoded_fmt_stream_map"].Split(',');
            list.Clear();
            foreach (string item in fmt_map)
            {
                list = HttpUtility.ParseQueryString(item);
                YutubFileFormat fmt = new YutubFileFormat();
                fmt.VideoCode = int.Parse(list["itag"]);
                fmt.DownloadURL = string.Format("{0}&signature={1}", list["url"], list["sig"]);
                fmt.VideoQu = list["quality"];
                FormatList.Add(fmt);
            }
        }
    }

    public class DownloadManager
    {
        #region Özellikler
        public string DownloadURL { get; set; }
        public string DownloadFileName { get; set; }
        public string DownloadPath { get; set; }
        bool downloading = false;
        int totalbytes = 0, readedbytes = 0;
        #endregion
        public event dlgDownlading onDownloading;
        Stream sr;
        FileStream fs;
        Thread tr1;


        public bool Start()
        {
            if (string.IsNullOrEmpty(DownloadURL)) return false;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(DownloadURL);
            req.Method = "GET";
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            totalbytes = int.Parse(res.GetResponseHeader("Content-Length"));
            sr = res.GetResponseStream();
            fs = new FileStream(string.Format("{0}\\{1}", DownloadPath, DownloadFileName), FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            downloading = true;
            tr1 = new Thread(new ThreadStart(Downloading));
            tr1.Start();
            return true;
        }

        void Downloading()
        {
            int n = 0, kalan = totalbytes;
            byte[] buffer = new byte[1001];
            while (kalan > 0 && downloading)
            {
                n = sr.Read(buffer, 0, 1000);
                fs.Write(buffer, 0, n);
                readedbytes += n;
                kalan -= n;
                if (onDownloading != null)
                    onDownloading(readedbytes, totalbytes);
            }
            sr.Close();
            fs.Flush();
            fs.Close();
            downloading = false;
            buffer = null;
            GC.Collect();
        }

        public bool Stop()
        {
            return downloading = false;
        }
    }
}
