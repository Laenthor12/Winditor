﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WindEditor.View;
using WindEditor.ViewModel;
using WindEditor.Events;
using NodeNetwork.Views;
using NodeNetwork.ViewModels;
using NodeNetwork.Toolkit.Layout.ForceDirected;
using System.Collections.ObjectModel;
using DynamicData;
using System.Windows.Input;
using OpenTK;

namespace WindEditor.Editor.Modes
{
    public partial class EventMode : IEditorMode
    {
        private DockPanel m_ModeControlsDock;
        private ComboBox m_EventCombo;

        private WDetailsViewViewModel m_EventDetailsViewModel;
        private WDetailsViewViewModel m_ActorDetailsViewModel;

        private EventNodeWindow m_NodeWindow;

        private WEventList m_EventList;
        private Event m_SelectedEvent;
        private Staff m_SelectedStaff;

        private Selection<BindingVector3> m_EditorSelection;

        public WDetailsViewViewModel EventDetailsViewModel
        {
            get { return m_EventDetailsViewModel; }
            set
            {
                if (value != m_EventDetailsViewModel)
                {
                    m_EventDetailsViewModel = value;
                    OnPropertyChanged("EventDetailsViewModel");
                }
            }
        }

        public WDetailsViewViewModel ActorDetailsViewModel
        {
            get { return m_ActorDetailsViewModel; }
            set
            {
                if (value != m_ActorDetailsViewModel)
                {
                    m_ActorDetailsViewModel = value;
                    OnPropertyChanged("ActorDetailsViewModel");
                }
            }
        }

        public DockPanel ModeControlsDock
        {
            get { return m_ModeControlsDock; }
            set
            {
                if (value != m_ModeControlsDock)
                {
                    m_ModeControlsDock = value;
                    OnPropertyChanged("ModeControlsDock");
                }
            }
        }

        public WEventList EventList
        {
            get { return m_EventList; }
            set
            {
                if (value != m_EventList)
                {
                    m_EventList = value;
                    OnPropertyChanged("EventList");
                }
            }
        }

        public Event SelectedEvent
        {
            get { return m_SelectedEvent; }
            set
            {
                if (value != m_SelectedEvent)
                {
                    m_SelectedEvent = value;
                    OnPropertyChanged("SelectedEvent");
                }
            }
        }

        public Staff SelectedStaff
        {
            get { return m_SelectedStaff; }
            set
            {
                if (value != m_SelectedStaff)
                {
                    m_SelectedStaff = value;
                    OnPropertyChanged("SelectedStaff");
                }
            }
        }

        public Selection<BindingVector3> EditorSelection
        {
            get { return m_EditorSelection; }
            set
            {
                if (m_EditorSelection != value)
                {
                    m_EditorSelection = value;
                    OnPropertyChanged("EditorSelection");
                }
            }
        }

        public WWorld World { get; }

        public event EventHandler<GenerateUndoEventArgs> GenerateUndoEvent;

        public EventMode(WWorld world)
        {
            World = world;
            EventDetailsViewModel = new WDetailsViewViewModel();
            ActorDetailsViewModel = new WDetailsViewViewModel();

            TransformGizmo = new WTransformGizmo(world);

            ModeControlsDock = CreateUI();

            m_NodeWindow = new EventNodeWindow();
            m_NodeWindow.ActorPropertiesView.DataContext = ActorDetailsViewModel;
            m_NodeWindow.ActorTabControl.SelectionChanged += OnSelectedActorChanged;
            m_NodeWindow.Closing += M_NodeWindow_Closing;

            EditorSelection = new Selection<BindingVector3>(this);
            EditorSelection.OnSelectionChanged += OnSelectionChanged;
        }

        private void M_NodeWindow_Closing(object sender, CancelEventArgs e)
        {
            m_NodeWindow.Hide();
            e.Cancel = true;
        }

        /// <summary>
        /// Generates a DockPanel that contains the controls specific to this editor mode.
        /// </summary>
        private DockPanel CreateUI()
        {
            DockPanel event_dock_panel = new DockPanel()
            {
                LastChildFill = true
            };

            StackPanel event_stack_panel = new StackPanel()
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
            };

            RowDefinition combo_row = new RowDefinition();
            RowDefinition properties_row = new RowDefinition();

            combo_row.Height = System.Windows.GridLength.Auto;
            combo_row.MaxHeight = 500;
            combo_row.MinHeight = 10;
            properties_row.Height = System.Windows.GridLength.Auto;
            //properties_row.MinHeight = 80;

            Grid ev_grid = new Grid();
            ev_grid.RowDefinitions.Add(combo_row);
            ev_grid.RowDefinitions.Add(properties_row);

            m_EventCombo = new ComboBox()
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                DisplayMemberPath = "Name"
            };

            m_EventCombo.SelectionChanged += OnEventSelectionChanged;

            Grid.SetRow(m_EventCombo, 0);

            WDetailsView actor_details = new WDetailsView()
            {
                Name = "Details",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                DataContext = EventDetailsViewModel
            };

            GroupBox actor_prop_box = new GroupBox()
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                Header = "Properties",
                Content = actor_details,
            };

            Grid.SetRow(actor_prop_box, 1);

            ev_grid.Children.Add(m_EventCombo);
            ev_grid.Children.Add(actor_prop_box);

            DockPanel.SetDock(event_stack_panel, Dock.Top);

            event_stack_panel.Children.Add(ev_grid);
            event_dock_panel.Children.Add(event_stack_panel);

            return event_dock_panel;
        }

        private void OnEventSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_EventCombo.SelectedIndex == -1)
            {
                SelectedEvent = m_EventList.Events[0];
            }
            else
            {
                SelectedEvent = EventList.Events[m_EventCombo.SelectedIndex];
            }

            m_EventDetailsViewModel.ReflectObject(SelectedEvent);

            m_NodeWindow.ActorTabControl.Items.Clear();

            foreach (Staff s in SelectedEvent.Actors)
            {
                if (s.StaffNodeGraph == null)
                {
                    s.StaffNodeGraph = GenerateActorTabItem(s);
                }

                m_NodeWindow.ActorTabControl.Items.Add(s.StaffNodeGraph);
            }

            m_NodeWindow.ActorTabControl.SelectedIndex = 0;
        }

        private void OnSelectedActorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_NodeWindow.ActorTabControl.SelectedIndex == -1)
            {
                SelectedStaff = null;
            }
            else
            {
                SelectedStaff = SelectedEvent.Actors[m_NodeWindow.ActorTabControl.SelectedIndex];
            }

            ActorDetailsViewModel.ReflectObject(SelectedStaff);
        }

        public void BroadcastUndoEventGenerated(WUndoCommand command)
        {
            GenerateUndoEvent?.Invoke(this, new GenerateUndoEventArgs(command));
        }

        public void ClearSelection()
        {
            //throw new NotImplementedException();
        }

        public void FilterSceneForRenderer(WSceneView view, WWorld world)
        {
            foreach (WScene scene in world.Map.SceneList)
            {
                foreach (var renderable in scene.GetChildrenOfType<IRenderable>())
                    renderable.AddToRenderer(view);
            }

            Staff camera = (Staff)SelectedEvent.Actors.ToList().Find(x => x.StaffType == StaffType.Camera);

            if (camera != null)
            {
                Cut c = camera.FirstCut;

                while (c != null)
                {
                    OpenTK.Vector3 eye_pos = new OpenTK.Vector3();
                    OpenTK.Vector3 target_pos = new OpenTK.Vector3();

                    Substance eye = c.Properties.Find(x => x.Name.ToLower() == "eye");
                    if (eye != null)
                    {
                        Substance<ObservableCollection<BindingVector3>> eye_vec = eye as Substance<ObservableCollection<BindingVector3>>;
                        eye_pos = eye_vec.Data[0].BackingVector;

                        WLinearColor draw_color = WLinearColor.White;
                        if (EditorSelection.SelectedObjects.Contains(eye_vec.Data[0]))
                            draw_color = WLinearColor.FromHexString("0xFF4F00FF");

                        world.DebugDrawBillboard("eye.png", eye_pos, new OpenTK.Vector3(100, 100, 100), draw_color, 0.025f);
                    }

                    Substance target = c.Properties.Find(x => x.Name.ToLower() == "center");
                    if (target != null)
                    {
                        Substance<ObservableCollection<BindingVector3>> target_vec = target as Substance<ObservableCollection<BindingVector3>>;
                        target_pos = target_vec.Data[0].BackingVector;

                        WLinearColor draw_color = WLinearColor.White;
                        if (EditorSelection.SelectedObjects.Contains(target_vec.Data[0]))
                            draw_color = WLinearColor.FromHexString("0xFF4F00FF");

                        world.DebugDrawBillboard("target.png", target_pos, new OpenTK.Vector3(100, 100, 100), draw_color, 0.025f);
                    }

                    if (eye != null && target != null)
                    {
                        world.DebugDrawLine(eye_pos, target_pos, WLinearColor.Black, 100000.0f, 0.025f);
                    }

                    c = c.NextCut;
                }
            }
        }

        public void OnBecomeActive()
        {
            WStage stage = (WStage)World.Map.SceneList.First(x => x.GetType() == typeof(WStage));
            EventList = stage.GetChildrenOfType<WEventList>()[0];

            if (m_EventCombo.ItemsSource != EventList.Events)
            {
                m_EventCombo.ItemsSource = EventList.Events;
                m_EventCombo.SelectedIndex = 0;
            }

            m_NodeWindow.Show();
        }

        public void OnBecomeInactive()
        {
            m_NodeWindow.Hide();
        }

        public void Update(WSceneView view)
        {
            UpdateSelectionGizmo(view);

            // Add our gizmo to the renderer this frame.
            if (TransformGizmo != null)
                ((IRenderable)TransformGizmo).AddToRenderer(view);

            // If we have a gizmo and we're transforming it, don't check for selection change.
            if (TransformGizmo != null && TransformGizmo.IsTransforming)
                return;
            if (WInput.GetMouseButtonDown(0) && !WInput.GetMouseButton(1))
            {
                FRay mouseRay = view.ProjectScreenToWorld(WInput.MousePosition);
                BindingVector3 addedVec = Raycast(mouseRay, view.ViewCamera);

                // Check the behaviour of this click to determine appropriate selection modification behaviour.
                // Click w/o Modifiers = Clear Selection, add result to selection
                // Click /w Ctrl = Toggle Selection State
                // Click /w Shift = Add to Selection
                bool ctrlPressed = WInput.GetKey(Key.LeftCtrl) || WInput.GetKey(Key.RightCtrl);
                bool shiftPressed = WInput.GetKey(Key.LeftShift) || WInput.GetKey(Key.RightShift);

                // Replace the previous selection with the current selection, if it's valid.
                if (!ctrlPressed & !shiftPressed)
                {
                    EditorSelection.ClearSelection();

                    if (addedVec != null)
                    {
                        EditorSelection.AddToSelection(addedVec);
                    }
                }
                else if (addedVec != null && (ctrlPressed && !shiftPressed))
                {
                    if (EditorSelection.SelectedObjects.Contains(addedVec))
                    {
                        EditorSelection.RemoveFromSelection(addedVec);
                    }
                    else
                    {
                        EditorSelection.AddToSelection(addedVec);
                    }
                }
                else if (addedVec != null && shiftPressed)
                {
                    if (!EditorSelection.SelectedObjects.Contains(addedVec))
                    {
                        EditorSelection.AddToSelection(addedVec);
                    }
                }
            }
            
            UpdateGizmoTransform();
        }

        private BindingVector3 Raycast(FRay ray, WCamera camera)
        {
            BindingVector3 selected_vec = null;
            List<BindingVector3> vecs_to_raycast = GetCameraVectorProperties();
            List<Tuple<float, BindingVector3>> results = new List<Tuple<float, BindingVector3>>();

            foreach (BindingVector3 bv in vecs_to_raycast)
            {
                FPlane plane = new FPlane(ray.Direction.Normalized(), bv.BackingVector);

                float dist = float.MaxValue;
                plane.RayIntersectsPlane(ray, out dist);

                Vector3 plane_intersect_point = camera.Transform.Position + (plane.Normal * dist);
                float distance_from_billboard_center = (plane_intersect_point - bv.BackingVector).Length;

                if (distance_from_billboard_center < 50.0f)
                {
                    float point_dist = (camera.Transform.Position - bv.BackingVector).Length;
                    results.Add(new Tuple<float, BindingVector3>(point_dist, bv));
                }
            }

            results.Sort(delegate(Tuple<float, BindingVector3> x, Tuple<float, BindingVector3> y)
            {
                return x.Item1.CompareTo(y.Item1);
            });

            if (results.Count > 0)
                selected_vec = results[0].Item2;

            return selected_vec;
        }

        private TabItem GenerateActorTabItem(Staff staff)
        {
            // Create view model for node network.
            NetworkViewModel model = new NetworkViewModel();

            // If this actor has at least one cut...
            if (staff.FirstCut != null)
            {
                staff.FirstCut.NodeViewModel.CreateNodesRecursive(model, null);
            }

            // Create the visual component of the node network.
            NetworkView v = new NetworkView()
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                ViewModel = model
            };

            v.ContextMenu = new ActorTabContextMenu(staff);

            model.SelectedNodes.Connect().Subscribe(d => { OnSelectedNodesChanged(d); });

            // Finally, create the new tab.
            TabItem new_tab = new TabItem() { Header = staff.Name, Content = v };
            return new_tab;
        }

        private void OnSelectedNodesChanged(IChangeSet<NodeViewModel> changeset)
        {
            Change<NodeViewModel> change = changeset.First();

            NodeViewModel view = change.Item.Current;
            if (view == null)
                return;

            if (view.Outputs.Count > 0 && view.Outputs.Items.First().Editor is VectorValueEditorViewModel vec_model)
            {
                switch(change.Reason)
                {
                    case ListChangeReason.Add:
                        EditorSelection.AddToSelection(vec_model.Value[0]);
                        break;
                    case ListChangeReason.Remove:
                        EditorSelection.RemoveFromSelection(vec_model.Value[0]);
                        break;
                }
            }
        }

        private void OnSelectionChanged()
        {
            // This will get invoked when an Undo happens which allows the gizmo to fix itself.
            UpdateGizmoTransform();
        }

        private List<BindingVector3> GetCameraVectorProperties()
        {
            List<BindingVector3> vecs = new List<BindingVector3>();

            Staff camera = (Staff)SelectedEvent.Actors.ToList().Find(x => x.StaffType == StaffType.Camera);

            if (camera != null)
            {
                Cut c = camera.FirstCut;

                while (c != null)
                {
                    OpenTK.Vector3 eye_pos = new OpenTK.Vector3();
                    OpenTK.Vector3 target_pos = new OpenTK.Vector3();

                    Substance eye = c.Properties.Find(x => x.Name.ToLower() == "eye");
                    if (eye != null)
                    {
                        Substance<ObservableCollection<BindingVector3>> eye_vec = eye as Substance<ObservableCollection<BindingVector3>>;
                        vecs.Add(eye_vec.Data[0]);
                    }

                    Substance target = c.Properties.Find(x => x.Name.ToLower() == "center");
                    if (target != null)
                    {
                        Substance<ObservableCollection<BindingVector3>> target_vec = target as Substance<ObservableCollection<BindingVector3>>;
                        vecs.Add(target_vec.Data[0]);
                    }

                    c = c.NextCut;
                }
            }

            return vecs;
        }

        #region INotifyPropertyChanged Support

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    TransformGizmo.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CollisionMode()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
