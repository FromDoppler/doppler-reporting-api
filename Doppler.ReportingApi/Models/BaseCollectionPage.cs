using System;
using System.Collections.Generic;

namespace Doppler.ReportingApi.Models
{
    public class BaseCollectionPage<T>
    {
        /// <summary>
        /// List of items for the current page.
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items in the collection.
        /// </summary>
        public int ItemsCount { get; set; }

        /// <summary>
        /// Current page number (zero-based).
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Total number of pages (calculated from ItemsCount and PageSize).
        /// </summary>
        public int PagesCount => PageSize > 0 ? (int)Math.Ceiling((double)ItemsCount / PageSize) : 0;
    }
}
