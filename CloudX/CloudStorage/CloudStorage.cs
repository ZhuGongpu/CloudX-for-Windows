using System;
using System.IO;
using System.Net;
using System.Threading;
using qiniu;

namespace CloudX.CloudStorage
{
    internal class CloudStorage
    {
        public static void Init()
        {
            // 初始化qiniu配置，主要是API Keys
            Config.ACCESS_KEY = "VDWt42uEpY7Q4ED4ZtePI_2XrD1WHwlbLhihPvei";
            Config.SECRET_KEY = "pLWHe6Qf3DCd3fMd_rkqbDJfMuK5AwT-Iqdmq4_X";
        }


        /// <summary>
        ///     将本地文件传到云端，云端key(即文件名)为本地路径（路径中的\均替换为/）
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="filePath">本地文件的路径</param>
        /// <param name="uploadCompleted"></param>
        /// <param name="uploadFailed"></param>
        /// <param name="progressChanged"></param>
        public static void UploadFile(string bucket, string filePath,
            EventHandler<QiniuUploadCompletedEventArgs> uploadCompleted,
            EventHandler<QiniuUploadFailedEventArgs> uploadFailed,
            EventHandler<QiniuUploadProgressChangedEventArgs> progressChanged)
        {
            var info = new FileInfo(filePath);
            if (!info.Exists) return;

            //替换路径中的\
            string fileName = info.FullName.Replace('\\', '/');

            var qfile = new QiniuFile(bucket, fileName, fileName);
            var puttedCtx = new ResumbleUploadEx(fileName);
            var done = new ManualResetEvent(false);

            qfile.UploadCompleted +=
                (sender, e) =>
                {
                    uploadCompleted(sender, e);
                    Console.WriteLine("Completed: key:{0}  hash:{1}", e.key, e.Hash);
                    done.Set();
                };
            qfile.UploadFailed += (sender, e) =>
            {
                uploadFailed(sender, e);
                Console.WriteLine(e.Error.ToString());
                puttedCtx.Save();
                done.Set();
            };

            qfile.UploadProgressChanged += (sender, e) =>
            {
                var percentage = (int) (100*e.BytesSent/e.TotalBytes);
                //TODO Console.WriteLine(percentage);

                progressChanged(sender, e);
            };
            qfile.UploadBlockCompleted += (sender, e) =>
            {
                puttedCtx.Add(e.Index, e.Ctx);
                puttedCtx.Save();
            };
            qfile.UploadBlockFailed += (sender, e) =>
            {
                //
            };

            //上传为异步操作
            //上传本地文件到七牛云存储
            //qfile.Upload (puttedCtx.PuttedCtx);
            qfile.Upload();
            done.WaitOne();
        }


        /// <summary>
        ///     下载公共资源
        /// </summary>
        /// <param name="sourceURL"></param>
        /// <param name="destinationPath"></param>
        public static void DownloadPublicResource(string sourceURL, string destinationPath)
        {
            var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);

            HttpWebResponse response;
            //TODO

            fileStream.Close();
        }
    }
}