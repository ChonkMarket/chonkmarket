namespace ChonkyWeb.Modelsl.V1ApiModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;

    public class V1Response
    {
        public string DataType { get; set; }
        public object Data { get; set; }
        public bool Success { get; set; } = true;
    }

    public class V1Response<T>
    {
        public string DataType { get; set; }
        public T Data { get; set; }
        public bool Success { get; set; }

        public V1Response() {
            DataType = typeof(T).Name;
        }
    }

    public class V1Error
    {
        [DefaultValue(false)]
        public bool Success { get; set; } = false;
        // Just future proofing, not actually using this for anything
        public int ErrorCode { get; set; } = -1;
        public string Message { get; set; }
    }
}
