using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Qiniu.Conf;
using Qiniu.IO;
using Qiniu.IO.Resumable;
using Qiniu.RPC;
using Qiniu.RS;
using Qiniu.RSF;

namespace CloudX.CloudStorage
{
    internal class CloudStorage
    {
        private const string AccessKey = "VDWt42uEpY7Q4ED4ZtePI_2XrD1WHwlbLhihPvei";
        private const string SecretKey = "pLWHe6Qf3DCd3fMd_rkqbDJfMuK5AwT-Iqdmq4_X";

        /// <summary>
        ///     初始化
        /// </summary>
        public static void Init()
        {
            Config.ACCESS_KEY = AccessKey;
            Config.SECRET_KEY = SecretKey;
        }

        /// <summary>
        ///     生成token
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="expireTime">token有效时间，单位为秒</param>
        /// <returns></returns>
        public static string GenToken(string bucket, uint expireTime = 3600)
        {
            return new PutPolicy(bucket, expireTime).Token();
        }

        /// <summary>
        ///     普通上传
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="filePath"></param>
        /// <param name="finished"></param>
        public static void PutFile(string bucket, string filePath, EventHandler<PutRet> finished)
        {
            string upToken = GenToken(bucket);
            var extra = new PutExtra();
            var client = new IOClient();
            client.PutFinished += finished; //必须放在执行client.PutFile之前

            client.PutFile(upToken, filePath, filePath, extra);
        }

        /// <summary>
        ///     断点续传
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="filePath"></param>
        /// <param name="progress"></param>
        /// <param name="finished"></param>
        /// <param name="failed"></param>
        public static void ResumablePutFile(string bucket, string filePath, Action<float> progress,
            EventHandler<CallRet> finished, EventHandler<CallRet> failed = null
            )
        {
            string upToken = GenToken(bucket);
            var setting = new Settings();
            var extra = new ResumablePutExtra();
            var client = new ResumablePut(setting, extra);
            client.Progress += progress;
            client.PutFinished += finished;
            client.PutFailure += failed;
            client.PutFile(upToken, filePath, filePath);
        }

        /// <summary>
        ///     获取存储空间中的文件列表
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="limit">返回结果的数量</param>
        /// <returns></returns>
        public static List<DumpItem> GetFileList(string bucket, int limit)
        {
            var client = new RSFClient(bucket);
            client.Limit = limit;
            DumpRet results = client.ListPrefix(bucket);

            //Console.WriteLine("FileListSize = {0}", results.Items.Count);
            //foreach (DumpItem dumpItem in results.Items)
            //{
            //    Console.WriteLine("Key:{0}  Hash:{1}  PutTime:{2}  EndUser:{3}", dumpItem.Key, dumpItem.Hash,
            //        dumpItem.PutTime, dumpItem.EndUser);
            //}
            //Console.WriteLine();

            return results.Items;
        }


        /// <summary>
        ///     从存储空间中删除文件
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool DeleteFile(string bucket, string key)
        {
            var client = new RSClient();
            CallRet ret = client.Delete(new EntryPath(bucket, key));
            return ret.OK;
        }

        /// <summary>
        ///     获取文件状态
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="key">文件名</param>
        /// <returns></returns>
        public static Entry GetFileState(string bucket, string key)
        {
            var client = new RSClient();
            return client.Stat(new EntryPath(bucket, key));
        }

        /// <summary>
        ///     下载公共资源,非异步
        /// </summary>
        /// <param name="sourceURL"></param>
        /// <param name="destinationPath"></param>
        public static void DownloadPublicResource(string sourceURL, string destinationPath)
        {
            try
            {
                var uri = new Uri(sourceURL);
                var request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "GET";
                request.ContentType = "application/x-www-form-urlencoded";

                var response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);

                long contentLength = response.ContentLength;
                //Console.WriteLine(contentLength);

                long receivedSize = 0;
                if (responseStream != null)
                    while (receivedSize < contentLength)
                    {
                        var buffer = new byte[1024];
                        receivedSize += responseStream.Read(buffer, 0, buffer.Length);
                        fileStream.Write(buffer, 0, buffer.Length);
                        fileStream.Flush();
                        //double progress = (double) receivedSize/contentLength;
                        //Console.WriteLine("Progress : {0:0.00}", progress);
                    }
                if (responseStream != null)
                    responseStream.Close();
                response.Close();
                fileStream.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// 生成公开资源下载连接
        /// </summary>
        /// <param name="bucket">云存储空间名称</param>
        /// <param name="key">云存储空间中的文件名</param>
        /// <returns></returns>
        public static string GenPublicResourceDownloadURL(string bucket, string key)
        {
            return String.Format("http://{0}.qiniudn.com/{1}", bucket, key);
        }
    }
}