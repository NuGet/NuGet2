using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace NuGet
{
    /// <remarks>
    /// Based on the blog post by Travis Illig at http://www.paraesthesia.com/archive/2009/12/16/posting-multipartform-data-using-.net-webrequest.aspx
    /// </remarks>
    public class MultipartWebRequest
    {
        
        private const string FormDataTemplate = "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}\r\n";
        private const string FileTemplate = "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n";
        private readonly Dictionary<string, string> _formData;

        private readonly List<PostFileData> _files;

        public MultipartWebRequest()
            : this(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))
        {
        }

        public MultipartWebRequest(Dictionary<string, string> formData)
        {
            _formData = formData;
            _files = new List<PostFileData>();
        }

        public void AddFormData(string key, string value)
        {
            _formData.Add(key, value);
        }

        public void AddFile(Func<Stream> fileFactory, string fieldName, string contentType = "application/octet-stream")
        {
            _files.Add(new PostFileData { FileFactory = fileFactory, FieldName = fieldName, ContentType = contentType });
        }

        public void CreateMultipartRequest(WebRequest request)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", CultureInfo.InvariantCulture);
            request.ContentType = "multipart/form-data; boundary=" + boundary;
			
            //byte[] byteContent;
            request.ContentLength = GetLength(request, boundary);
            using (Stream stream = request.GetRequestStream())
            {
                foreach (var item in _formData)
                {
                    string header = String.Format(CultureInfo.InvariantCulture, FormDataTemplate, boundary, item.Key, item.Value);
                    byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                    stream.Write(headerBytes, 0, headerBytes.Length);
                }

                byte[] newlineBytes = Encoding.UTF8.GetBytes("\r\n");
                foreach (var file in _files)
                {
                    string header = String.Format(CultureInfo.InvariantCulture, FileTemplate, boundary, file.FieldName, file.FieldName, file.ContentType);
                    byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                    stream.Write(headerBytes, 0, headerBytes.Length);

                    Stream fileStream = file.FileFactory();
                    byte[] buffer = new byte[1024];
                    int bytesRead = 0;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                    } // end while
                    fileStream.Close();
                    stream.Write(newlineBytes, 0, newlineBytes.Length);
                }
                string trailer = String.Format(CultureInfo.InvariantCulture, "--{0}--", boundary);
                byte[] trailerBytes = Encoding.UTF8.GetBytes(trailer);
                stream.Write(trailerBytes, 0, trailerBytes.Length);
                
            }
          
        }

        private int GetLength(WebRequest request, string boundary)
        {

            //string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", CultureInfo.InvariantCulture);
            request.ContentType = "multipart/form-data; boundary=" + boundary;

            //byte[] byteContent;
            int byteLength = 0;

                foreach (var item in _formData)
                {
                    string header = String.Format(CultureInfo.InvariantCulture, FormDataTemplate, boundary, item.Key, item.Value);
                    byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                    
                    byteLength += headerBytes.Length;
                }

                byte[] newlineBytes = Encoding.UTF8.GetBytes("\r\n");
                foreach (var file in _files)
                {
                    string header = String.Format(CultureInfo.InvariantCulture, FileTemplate, boundary, file.FieldName, file.FieldName, file.ContentType);
                    byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                    
                    byteLength += headerBytes.Length;

                    Stream fileStream = file.FileFactory();
                    byte[] buffer = new byte[1024];
                    int bytesRead = 0;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        
                        byteLength += bytesRead;
                    } // end while
                    fileStream.Close();
                    byteLength += newlineBytes.Length;
                }
                string trailer = String.Format(CultureInfo.InvariantCulture, "--{0}--", boundary);
                byte[] trailerBytes = Encoding.UTF8.GetBytes(trailer);
                
                byteLength += trailerBytes.Length;


            

            return byteLength;
        }

        private sealed class PostFileData
        {
            public Func<Stream> FileFactory { get; set; }

            public string ContentType { get; set; }

            public string FieldName { get; set; }
        }
    }
}
