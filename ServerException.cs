using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XdsObjects.Enums;

namespace XdsRegistry
{
    internal class ServerException : Exception
    {
        public XdsErrorCode StatusCode { get; set; }
        public ServerException(string Message, XdsErrorCode statusCode)
            : base(Message)
        {
            StatusCode = statusCode;
        }
    }
}
