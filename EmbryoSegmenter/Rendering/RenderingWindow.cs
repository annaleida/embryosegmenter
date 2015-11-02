using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kitware.VTK;
using EmbryoSegmenter.Shapes;

namespace EmbryoSegmenter.Rendering
{
    class RenderingWindow
    {
        static vtkRenderer _renderer;
        static vtkRenderWindow _render_window;
        static vtkRenderWindowInteractor _render_window_interactor;
        List<vtkActor> _all_actors = new List<vtkActor>();
        List<vtkPolyData> _all_polydata;
        List<vtkCellArray> _all_vertices;
        List<vtkPoints> _all_points;
        List<vtkDataSetMapper> _all_mappers;
        vtkSphereSource testSphere;

        public RenderingWindow()
        {
            _renderer = vtkRenderer.New();
            _render_window = vtkRenderWindow.New();
            _render_window_interactor = vtkRenderWindowInteractor.New();
            _render_window.AddRenderer(_renderer);
            _render_window_interactor.SetRenderWindow(_render_window);
            _all_vertices = new List<vtkCellArray>();
            _all_points = new List<vtkPoints>();
            _all_mappers = new List<vtkDataSetMapper>();
            _all_polydata = new List<vtkPolyData>();
        }

        /// <summary>
        /// Add whatever shapes you want drawn.
        /// Should transform any supported shape into actors
        /// Should be made a little more dynamic, let you choose Delauney and so on
        /// </summary>
        public void SetShapes(List<Blob> blobs)
        {
            foreach (Blob blob in blobs)
            {
                vtkCellArray _vertices = vtkCellArray.New();
                vtkPoints _points = vtkPoints.New();
                int index = 1;
                foreach (Segment _seg in blob.segments)
                {
                    foreach (Point _p in _seg.points)
                    {
                        _points.InsertNextPoint(_p.X, _p.Y, _p.Z);
                        vtkVertex _vertex = vtkVertex.New();
                        _vertex.GetPointIds().SetId(0, index);
                        _vertices.InsertNextCell(_vertex);
                        index++;
                    }
                }

                vtkPolyData polydata = vtkPolyData.New();
                polydata.SetPoints(_points);
                polydata.SetVerts(_vertices);
                _all_vertices.Add(_vertices);
                _all_points.Add(_points);
                _all_polydata.Add(polydata);
            }
           
        }

        private vtkActor Test(vtkPolyData polydata)
        {
            vtkActor actor = vtkActor.New();
            testSphere = vtkSphereSource.New();
            vtkDataSetMapper mapper = vtkDataSetMapper.New();
            mapper.SetInputConnection(testSphere.GetOutputPort());
            actor.SetMapper(mapper);
            _all_mappers.Add(mapper);
            
            return actor;
        }

        private vtkActor PointActor(vtkPolyData polydata)
        {
            vtkActor actor = vtkActor.New();
            vtkDataSetMapper mapper = vtkDataSetMapper.New();
            mapper.SetInputConnection(polydata.GetProducerPort());
            actor.SetMapper(mapper);
            _all_mappers.Add(mapper);
            return actor;
        }

        private vtkActor DelauneyActor(vtkPolyData polydata)
        {
            vtkActor actor = vtkActor.New();
            vtkDelaunay3D delny = vtkDelaunay3D.New();
            delny.SetInput(polydata);
            vtkDataSetMapper mapper = vtkDataSetMapper.New();
            mapper.SetInputConnection(delny.GetOutputPort());
            actor.SetMapper(mapper);
            _all_mappers.Add(mapper);
            delny.Dispose();
            return actor;
        }

        private void ConstructActors(DisplayMode mode)
        {
            foreach (vtkPolyData poly in _all_polydata)
            {
                vtkActor actor = vtkActor.New();
                switch (mode)
                {
                    case DisplayMode.POINT:
                        actor = PointActor(poly);
                        break;
                    case DisplayMode.BODY:
                        actor = DelauneyActor(poly);
                        break;
                }
                if (actor != null)
                {
                    _all_actors.Add(actor);
                }
            }
          
            
        }

        /// <summary>
        /// The usual rendering stuff
        /// </summary>
        public void Show(DisplayMode mode)
            
        {
            ConstructActors(mode);
            if ((_all_actors == null) || (_all_actors.Count == 0))
            {
                return;
            }
            if (_renderer == null)
            {
                return;
            }
            int colorChoice = 0;
            foreach (vtkActor actor in _all_actors)
            {
                if (1 == 1)
                {
                    colorChoice++;
                    if (colorChoice == 6)
                    {
                        actor.GetProperty().SetColor(1, 0, 1);
                        colorChoice = 0;
                    }
                    else if (colorChoice == 1)
                    {
                        actor.GetProperty().SetColor(0, 1, 0);
                    }
                    else if (colorChoice == 2)
                    {
                        actor.GetProperty().SetColor(0, 0.3, 0);
                    }
                    else if (colorChoice == 3)
                    {
                        actor.GetProperty().SetColor(0, 0, 1);
                    }
                    else if (colorChoice == 4)
                    {
                        actor.GetProperty().SetColor(0, 0.3, 0);
                    }
                    else if (colorChoice == 5)
                    {
                        actor.GetProperty().SetColor(0, 0, 1);
                    }
                }
                //actor.GetProperty().SetOpacity(0.6);
                _renderer.AddActor(actor);
                
            }
            _renderer.SetBackground(0, 0, 0);
            _render_window.SetSize(500, 500);
            _render_window_interactor.Initialize();
            _render_window.Render();
            _render_window_interactor.Start();
            CleanUp();
        }

        /// <summary>
        /// Get rid of everything
        /// </summary>
        private void CleanUp()
        {
            if (_all_polydata != null)
            {
                foreach (vtkPolyData polydata in _all_polydata)
                {
                    polydata.Dispose();
                }
            }
            if (_all_actors != null)
            {
                foreach (vtkActor actor in _all_actors)
                {
                    actor.Dispose();
                }
            }
            if (_all_vertices != null)
            {
                foreach (vtkCellArray vertices in _all_vertices)
                {
                    vertices.Dispose();
                }
            }
            if (_all_points != null)
            {
                foreach (vtkPoints point in _all_points)
                {
                    point.Dispose();
                }
            }
            if (_render_window != null) { _render_window.Dispose(); }
            if (_render_window_interactor != null) { _render_window_interactor.Dispose(); }
            if (_renderer != null) { _renderer.Dispose(); }
        }
    }

    public enum DisplayMode
    {
        POINT = 0, BODY = 1
    }
}
