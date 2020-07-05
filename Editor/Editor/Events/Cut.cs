﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using WindEditor.Properties;
using GameFormatReader.Common;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;

namespace WindEditor.Events
{
    [HideCategories()]
    public class Cut : INotifyPropertyChanged
    {
        public List<BaseSubstance> Properties { get; private set; }

        private string m_Name;

        private int m_DuplicateID;

        private int[] m_CheckFlags = new int[3];
        private int m_Flag;

        private int m_FirstSubstanceIndex;
        private int m_NextCutIndex;

        private Staff m_ParentActor;

        private Cut m_NextCut;
        private Cut[] m_BlockingCuts = new Cut[3];

        private CutNodeViewModel m_NodeViewModel;

        public Staff ParentActor
        {
            get { return m_ParentActor; }
            set
            {
                if (value != m_ParentActor)
                {
                    m_ParentActor = value;
                    OnPropertyChanged("ParentActor");
                }
            }
        }

        public Cut NextCut
        {
            get { return m_NextCut; }
            set
            {
                if (value != m_NextCut)
                {
                    m_NextCut = value;
                    OnPropertyChanged("NextCut");
                }
            }
        }

        public Cut[] BlockingCuts
        {
            get { return m_BlockingCuts; }
            set
            {
                if (value != m_BlockingCuts)
                {
                    m_BlockingCuts = value;
                    OnPropertyChanged("BlockingCuts");
                }
            }
        }

        public CutNodeViewModel NodeViewModel
        {
            get { return m_NodeViewModel; }
            set
            {
                if (m_NodeViewModel != value)
                {
                    m_NodeViewModel = value;
                    OnPropertyChanged("NodeViewModel");
                }
            }
        }

        [WProperty("Cut", "Name", true, "Name of the action.")]
        public string Name
        {
            get { return m_Name; }
            set
            {
                if (value != m_Name)
                {
                    m_Name = $"{value}";
                    OnPropertyChanged("Name");
                }
            }
        }

        public Cut()
        {
            Properties = new List<BaseSubstance>();

            Name = "new_cut";
            NextCut = null;

            NodeViewModel = new CutNodeViewModel(this) { Name = this.Name };
        }

        public Cut(EndianBinaryReader reader, List<BaseSubstance> substances)
        {
            Properties = new List<BaseSubstance>();
            NextCut = null;

            Name = new string(reader.ReadChars(32)).Trim('\0');
            m_DuplicateID = reader.ReadInt32();

            reader.SkipInt32();

            m_CheckFlags[0] = reader.ReadInt32();
            m_CheckFlags[1] = reader.ReadInt32();
            m_CheckFlags[2] = reader.ReadInt32();

            m_Flag = reader.ReadInt32();

            m_FirstSubstanceIndex = reader.ReadInt32();
            m_NextCutIndex = reader.ReadInt32();

            reader.Skip(16);

            NodeViewModel = new CutNodeViewModel(this) { Name = this.Name };

            if (m_FirstSubstanceIndex != -1)
            {
                BaseSubstance subs = substances[m_FirstSubstanceIndex];

                while (subs != null)
                {
                    Properties.Add(subs);
                    subs = subs.NextSubstance;
                }
            }
        }

        public void AssignCutReferences(List<Cut> cut_list)
        {
            if (m_NextCutIndex != -1)
            {
                NextCut = cut_list[m_NextCutIndex];
            }

            for (int i = 0; i < 3; i++)
            {
                if (m_CheckFlags[i] != -1)
                {
                    BlockingCuts[i] = cut_list.Find(x => x.m_Flag == m_CheckFlags[i]);
                }
            }
        }

        public bool IsBlocked()
        {
            return m_BlockingCuts[0] != null || m_BlockingCuts[1] != null || m_BlockingCuts[2] != null;
        }

        public override string ToString()
        {
            return $"Name: \"{ Name }\", Property Count: { Properties.Count }";
        }

        #region INotifyPropertyChanged Support

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
