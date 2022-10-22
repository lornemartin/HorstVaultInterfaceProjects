using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RadanMaster.Models
{
    class DisplayItem
    {
        public int QtyRequired { get; set; }
        public int QtyNested { get; set; }
        public byte[] Thumbnail { get; set; }
        public string PartName { get; set; }
        public double Thickness { get; set; }
        public string Description { get; set; }
        public string Material { get; set; }
        public bool IsComplete { get; set; }
        public bool IsBatch { get; set; }
        public string ScheduleName { get; set; }
        public string BatchName { get; set; }
        public string OrderNumber { get; set; }
        public DateTime DateEntered { get; set; }
        public string Notes { get; set; }
        public bool IsInProject { get; set; }
        public bool OrderIsComplete { get; set; }
        public List<DisplayNest> AssociatedNests { get; set; }
        public OrderItem OrderItem { get; set; }

        public DisplayItem(OrderItem item)
        {
            QtyRequired = item.QtyRequired;
            QtyNested = item.QtyNested;
            Thumbnail = item.Part.Thumbnail;
            PartName = item.Part.FileName;
            Thickness = item.Part.Thickness;
            Description = item.Part.Description;
            Material = item.Part.Material;
            IsComplete = item.IsComplete;
            IsBatch = item.Order.IsBatch;
            ScheduleName = item.Order.ScheduleName;
            BatchName = item.Order.BatchName;
            OrderNumber = item.Order.OrderNumber;
            DateEntered = item.Order.EntryDate;
            Notes = item.Notes;
            IsInProject = item.IsInProject;
            OrderIsComplete = item.Order.IsComplete;

            List<DisplayNest> displayNests = new List<DisplayNest>();
            if (item.AssociatedNests.Count > 0)
            {
                foreach (Nest nest in item.AssociatedNests)
                {
                    DisplayNest displayNest = new DisplayNest();
                    displayNest.NestName = nest.nestName;
                    displayNest.NestPath = nest.nestPath;
                    displayNest.QtyOnNest = 0;
                    foreach (NestedParts nestedPart in nest.NestedParts)
                    {
                        if (nestedPart.OrderItem == item)
                        {
                            displayNest.QtyOnNest += nestedPart.Qty;
                        }
                    }
                    displayNests.Add(displayNest);
                }

                AssociatedNests = displayNests;
            }
            OrderItem = item;

        }
    }

    class DisplayNest
    {
        public string NestName { get; set; }
        public string NestPath { get; set; }
        public int QtyOnNest { get; set; }
        public byte[] Thumbnail { get; set; }
    }
}
