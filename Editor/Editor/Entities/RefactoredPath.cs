﻿using JStudio.J3D;
using OpenTK;
using System;
using System.Windows.Data;
using System.Globalization;
using System.Collections.Generic;
using WindEditor.ViewModel;

namespace WindEditor
{
    public partial class Path_v2
    {
        [WProperty("Path Properties", "Name", true)]
        override public string Name
        {
            get { return m_Name; }
            set
            {
                m_Name = value;
                OnPropertyChanged("Name");
            }
        }

        private string m_Name;

        [WProperty("Path Properties", "First Point", true)]
        public PathPoint_v2 FirstNode
        {
            get { return m_FirstNode; }
            set
            {
                if (value != m_FirstNode)
                {
                    m_FirstNode = value;
                    OnPropertyChanged("FirstNode");
                }
            }
        }

        private PathPoint_v2 m_FirstNode;

        public override string ToString()
        {
            return Name;
        }

        public override void PostLoad()
        {
            base.PostLoad();

            // ToDo: Get the index of our first node, into the array of passed in entities.
            // assign that as our FirstNode, and then recursively walk the children assigning their next node,
            // etc. until we hit a end-of-path node. 
        }

        public void SetNodes(List<WDOMNode> points)
        {
            int first_index = m_FirstEntryOffset / 16;
            if (first_index >= points.Count)
                return;

            FirstNode = (PathPoint_v2)points[first_index];
            FirstNode.Name = Name + $"_{0}";

            PathPoint_v2 cur_node = FirstNode;

            for (int i = 1; i < m_NumberofPoints; i++)
            {
                int next_index = first_index + i;
                if (next_index >= points.Count)
                    break;

                cur_node.NextNode = (PathPoint_v2)points[next_index];
                cur_node.NextNode.Name = Name + $"_{i}";
                cur_node = cur_node.NextNode;
            }
        }

        public List<PathPoint_v2> GetPoints()
        {
            List<PathPoint_v2> points = new List<PathPoint_v2>();

            PathPoint_v2 nextPoint = FirstNode;
            while (nextPoint != null)
            {
                points.Add(nextPoint);
                nextPoint = nextPoint.NextNode;
                if (nextPoint == FirstNode)
                    break; // Prevent infinite loops
            }

            return points;
        }

        public void SetNodeOffset(List<SerializableDOMNode> points)
        {
        	int first_index = points.IndexOf(FirstNode);
        	if(first_index < 0)
        	{
        		Console.WriteLine("Warning: Path has no first point assigned, defaulting to point 0");
                first_index = 0;
        	}

            m_FirstEntryOffset = first_index * 16;

            m_NumberofPoints = (short)GetPoints().Count;
        }
    }
}
