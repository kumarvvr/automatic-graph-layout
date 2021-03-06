/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.DataStructures;
using GeometryEdge = Microsoft.Msagl.Core.Layout.Edge;
using DrawingEdge = Microsoft.Msagl.Drawing.Edge;
using GeometryNode = Microsoft.Msagl.Core.Layout.Node;
using DrawingNode = Microsoft.Msagl.Drawing.Node;
namespace Microsoft.Msagl.GraphControlSilverlight
{
    /// <summary>
    /// This class represents a rendering node. It contains the Microsoft.Msagl.Drawing.Node and Microsoft.Msagl.Node.
    /// The rendering node is a XAML user control. Its template is stored in Themes/generic.xaml.
    /// </summary>
    public class DNode : DObject, IViewerNode, IHavingDLabel
    {
        private DLabel _Label;
        /// <summary>
        /// Gets or sets the rendering label.
        /// </summary>
        public DLabel Label
        {
            get
            {
                return _Label;
            }
            set
            {
                _Label = value;
                // Also set the drawing label and geometry label.
                DrawingNode.Label = value.DrawingLabel;
                //DrawingNode.Attr.GeometryNode.Label = value.DrawingLabel.GeometryLabel; // NEWMSAGL no label in the geometry node??
            }
        }

        /// <summary>
        /// the corresponding drawing node
        /// </summary>
        public Microsoft.Msagl.Drawing.Node DrawingNode { get; set; }

        public IEnumerable<IViewerEdge> OutEdges { get { foreach (DEdge e in _OutEdges) yield return e; } }
        public IEnumerable<IViewerEdge> InEdges { get { foreach (DEdge e in _InEdges) yield return e; } }
        public IEnumerable<IViewerEdge> SelfEdges { get { foreach (DEdge e in _SelfEdges) yield return e; } }
        public IEnumerable<DEdge> Edges
        {
            get
            {
                foreach (DEdge e in _OutEdges)
                    yield return e;
                foreach (DEdge e in _InEdges)
                    yield return e;
                foreach (DEdge e in _SelfEdges)
                    yield return e;
            }
        }
        internal List<DEdge> _OutEdges { get; private set; }
        internal List<DEdge> _InEdges { get; private set; }
        internal List<DEdge> _SelfEdges { get; private set; }

        internal DNode(DObject graph, DrawingNode drawingNode)
            : base(graph)
        {
            this.DrawingNode = drawingNode;
            _OutEdges = new List<DEdge>();
            _InEdges = new List<DEdge>();
            _SelfEdges = new List<DEdge>();
            PortsToDraw = new Set<Port>();

            if (drawingNode.Label != null && drawingNode.Label.GeometryLabel != null)
                Label = new DTextLabel(this, drawingNode.Label);

            if (!(this is DCluster))
                Canvas.SetZIndex(this, 10000);
        }

        public override void MakeVisual()
        {
            if (GeometryNode == null || GeometryNode.BoundaryCurve == null)
                return;
            SetValue(Canvas.LeftProperty, Node.BoundingBox.Left);
            SetValue(Canvas.TopProperty, Node.BoundingBox.Bottom);

            // Note that Draw.CreateGraphicsPath returns a curve with coordinates in graph space.
            var pathFigure = Draw.CreateGraphicsPath((DrawingNode.GeometryNode).BoundaryCurve);
            pathFigure.IsFilled = Node.Attr.FillColor.A != 0;
            pathFigure.IsClosed = true;
            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);
            // Apply a translation to bring the coordinates in node space (i.e. top left corner is 0,0).
            pathGeometry.Transform = new TranslateTransform() { X = -Node.BoundingBox.Left, Y = -Node.BoundingBox.Bottom };

            // I'm using the max of my LineWidth and MSAGL's LineWidth; this way, when the MSAGL bug is fixed (see below), my workaround shouldn't
            // interfere with the fix.
            var path = new Path()
            {
                Data = pathGeometry,
                StrokeThickness = Math.Max(LineWidth, Node.Attr.LineWidth),
                Stroke = BoundaryBrush,
                Fill = new SolidColorBrush(Draw.MsaglColorToDrawingColor(Node.Attr.FillColor)),
                StrokeLineJoin = PenLineJoin.Miter
            };
            Content = path;
        }

        // Workaround for MSAGL bug which causes Node.Attr.LineWidth to be ignored (it always returns 1 unless the GeometryNode is null).
        public int LineWidth { get; set; }

        /// <summary>
        /// Gets or sets the boundary brush.
        /// </summary>
        public Brush BoundaryBrush
        {
            get { return (Brush)GetValue(BoundaryBrushProperty); }
            set { SetValue(BoundaryBrushProperty, value); }
        }

        public static readonly DependencyProperty BoundaryBrushProperty =
            DependencyProperty.Register("BoundaryBrush",
            typeof(Brush),
            typeof(DNode),
            new PropertyMetadata(DGraph.BlackBrush));

        internal void AddOutEdge(DEdge edge)
        {
            _OutEdges.Add(edge);
        }

        internal void AddInEdge(DEdge edge)
        {
            _InEdges.Add(edge);
        }

        internal void AddSelfEdge(DEdge edge)
        {
            _SelfEdges.Add(edge);
        }

        /// <summary>
        /// return the color of a node
        /// </summary>
        public System.Windows.Media.Color Color
        {
            get { return Draw.MsaglColorToDrawingColor(this.DrawingNode.Attr.Color); }
        }

        /// <summary>
        /// Fillling color of a node
        /// </summary>
        public System.Windows.Media.Color FillColor
        {
            get { return Draw.MsaglColorToDrawingColor(this.DrawingNode.Attr.FillColor); }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetStrokeFill() { }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public GeometryNode GeometryNode
        {
            get { return this.DrawingNode.GeometryNode; }
        }

        /// <summary>
        /// returns the corresponding DrawingNode
        /// </summary>
        public DrawingNode Node
        {
            get { return this.DrawingNode; }
        }

        /// <summary>
        /// returns the corresponding drawing object
        /// </summary>
        override public DrawingObject DrawingObject
        {
            get { return Node; }
        }

        internal void RemoveOutEdge(DEdge de)
        {
            _OutEdges.Remove(de);
        }

        internal void RemoveInEdge(DEdge de)
        {
            _InEdges.Remove(de);
        }

        internal void RemoveSelfEdge(DEdge de)
        {
            _SelfEdges.Remove(de);
        }

        internal Set<Port> PortsToDraw { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public void AddPort(Port port)
        {
            PortsToDraw.Insert(port);
        }

        /// <summary>
        /// it is a class holding Microsoft.Msagl.Drawing.Node and Microsoft.Msagl.Node
        /// </summary>
        public void RemovePort(Port port)
        {
            if (port != null)
                PortsToDraw.Remove(port);
        }

        // not used at the moment; not sure what it does
        public event Action<IViewerNode> IsCollapsedChanged;
    }
}