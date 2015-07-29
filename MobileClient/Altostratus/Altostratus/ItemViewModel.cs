using Altostratus.Model;
using Altostratus.Properties;
using System;
using System.Diagnostics;

namespace Altostratus
{
    class ItemViewModel
    {
        DataModel dataModel;
        Item item = null;

        public ItemViewModel(Item item)
        {
            dataModel = App.DataModel;            

            // If we failed to get the item, use a substitute. This could only happen if the user manages to 
            // invoke an item that's been removed from the database. This shouldn't happen because the ListView
            // source is always queried from the database before the cleanup process starts (see SyncItems in
            // in sync.cs), and because the only items that get removed are those that should already fall outside
            // the limit of the item query. Nevertheless, this check remains for robustness.
            if (item.Body == null)
            {
                item.Body = Resources.Item_RemovedMessage;
                Debug.WriteLine("Item page invoked for item that was removed from the database: " + item.Url);                
            }

            this.item = item;
        }

        public String PageTitle { get { return Resources.Item_PageTitle; } }
        public String Title { get { return item.Title; } }
        public String Description { get { return item.Description; } }
        public String Body { get { return item.Body; } }
        public String Provider { get { return item.Provider; } }
        public String Url { get { return item.Url; } }
    }
}
