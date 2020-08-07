using OpenTK.Input;
using System;
using System.Collections.Generic;

namespace BrewLib.UserInterface
{
    public class DragDropBehavior : IDisposable
    {
        private delegate void DropOntoDelegate(Widget target, Draggable draggable);
        public delegate object GetDropDataDelegate();
        public delegate void ReceiveDelegate(object dropData);

        private readonly List<Draggable> draggables = new List<Draggable>();
        private readonly List<DropTarget> dropTargets = new List<DropTarget>();

        public void AddDraggable(Widget widget, GetDropDataDelegate getDropData)
        {
            var draggable = new Draggable
            {
                Widget = widget,
                GetDropData = getDropData,
                DropOnto = dropOnto,
            };
            draggable.Bind();
            draggables.Add(draggable);
        }

        public void AddDropTarget(Widget widget, ReceiveDelegate receive)
        {
            var dropTarget = new DropTarget
            {
                Widget = widget,
                Receive = receive,
            };
            dropTargets.Add(dropTarget);
        }

        private void dropOnto(Widget target, Draggable draggable)
        {
            foreach (var dropTarget in dropTargets)
            {
                if (dropTarget.Widget != target && !target.HasAncestor(dropTarget.Widget))
                    continue;

                dropTarget.Receive(draggable.GetDropData());
                break;
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var draggable in draggables)
                        draggable.Unbind();
                    draggables.Clear();
                    dropTargets.Clear();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        private class Draggable
        {
            private bool dragged;

            public Widget Widget;
            public GetDropDataDelegate GetDropData;
            public DropOntoDelegate DropOnto;

            public void Bind()
            {
                Widget.OnDrag += widget_OnDrag;
                Widget.OnClickDown += widget_OnClickDown;
                Widget.OnClickUp += widget_OnClickUp;
            }

            public void Unbind()
            {
                Widget.OnDrag -= widget_OnDrag;
                Widget.OnClickDown -= widget_OnClickDown;
                Widget.OnClickUp -= widget_OnClickUp;
            }

            private bool widget_OnClickDown(WidgetEvent evt, MouseButtonEventArgs e)
            {
                if (e.Button != MouseButton.Left) return false;
                dragged = true;
                return true;
            }

            private void widget_OnDrag(WidgetEvent evt, MouseMoveEventArgs e)
            {
                if (!dragged) return;

            }

            private void widget_OnClickUp(WidgetEvent evt, MouseButtonEventArgs e)
            {
                if (!dragged || e.Button != MouseButton.Left) return;
                dragged = false;

                if (evt.RelatedTarget == Widget)
                    return;

                DropOnto(evt.RelatedTarget, this);
            }
        }

        private class DropTarget
        {
            public Widget Widget;
            public ReceiveDelegate Receive;
        }
    }
}
