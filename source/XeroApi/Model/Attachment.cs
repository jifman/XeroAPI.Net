﻿using System;
using System.IO;
using System.Xml.Serialization;
using DevDefined.OAuth.Utility;
using System.ComponentModel.DataAnnotations;

namespace XeroApi.Model
{
    public class Attachment : ModelBase
    {
        public Attachment()
        {
        }

        public Attachment(FileInfo fileInfo)
        {
            if (fileInfo == null) { throw new ArgumentNullException("fileInfo");}
            if (!fileInfo.Exists) { throw new FileNotFoundException("The file could not be found", fileInfo.FullName);}

            Filename = fileInfo.Name;
            ContentLength = (int)fileInfo.Length;
            MimeType = MimeTypes.GetMimeType(fileInfo);
            ContentStream = fileInfo.OpenRead();
        }

        [Key]
        [ItemId]
        public Guid? AttachmentID { get; set; }

        [ItemNumber]
        public string Filename { get; set; }

        public string MimeType { get; set; }

        public int ContentLength { get; set; }

        public byte[] Content 
        { 
            get
            {
                if (ContentStream == null)
                    return new byte[0];

                if (ContentStream is MemoryStream)
                    return (ContentStream as MemoryStream).ToArray();

                MemoryStream ms = new MemoryStream(ContentLength);

                ContentStream.CopyTo(ms);

                return ms.ToArray();
            } 
            set
            {
                if (ContentStream != null)
                    ContentStream.Close();

                ContentStream = new MemoryStream(value);
            } 
        }

        [XmlIgnore]
        internal Stream ContentStream { get; set; }

        public Attachment WithContent(Stream contentStream)
        {
            ContentStream = contentStream;
            return this;
        }
    }


    public class Attachments : ModelList<Attachment>
    {
    }
}
