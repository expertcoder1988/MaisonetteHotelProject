using System;
using System.Collections.Generic;

namespace HotelWebsite.Models
{
    public class JQDTParams<T>
    {
        public JQDTParams()
        {
            length = 10;
            //start = 1;
            search = new JQDTColumnSearch(); 
        }

        public int draw { get; set; }

        //int _start = 1;
        //public int start
        //{
        //    get { return this._start == 0 ? 1 : this._start; }
        //    set { this._start = value; }
        //}

        public int start { get; set; }

        public int page { get { return (int)(Math.Ceiling((double)start / length) + 1); } }

        public int length { get; set; }
        public JQDTColumnSearch search { get; set; }
        public List<JQDTColumnOrder> order { get; set; }
        public List<JQDTColumn> columns { get; set; }

        public IEnumerable<T> data { get; set; }
        public int recordsTotal { get; set; }
        public int recordsFiltered { get; set; }

        // Extra Property as per your requirement
        public PostStatus postStatus { get; set; }
        public bool? isSubscribe { get; set; }
        public string tag { get; set; }
        public string previousPagingUrl { get; set; }
        public string nextPagingUrl { get; set; }

        public int totalPages
        {
            get
            {
                return (int)Math.Ceiling((double)recordsFiltered / length);
            }
        }
    }

    public enum JQDTColumnOrderDirection
    {
        asc, desc
    }

    public class JQDTColumnOrder
    {
        public int column { get; set; }
        public JQDTColumnOrderDirection dir { get; set; }
    }
    public class JQDTColumnSearch
    {
        public string value { get; set; }
        public string regex { get; set; }
    }

    public class JQDTColumn
    {
        public string data { get; set; }
        public string name { get; set; }
        public Boolean searchable { get; set; }
        public Boolean orderable { get; set; }
        public JQDTColumnSearch search { get; set; }
    }
}
