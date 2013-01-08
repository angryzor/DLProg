using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Threading;

namespace DLProg
{
    [Serializable]
    public class LinkQueue : ObservableCollection<Link>
    {
        [NonSerialized] private Dispatcher d;

        public LinkQueue(Dispatcher d)
        {
            this.d = d;
        }

        public void SetDispatcher(Dispatcher d)
        {
            this.d = d;
        }

        public Link Pop()
        {
            Monitor.Enter(this);
            Link l = null;
            d.Invoke((Action)(() =>
            {
                if (Count != 0)
                {
                    l = this[0];
                    RemoveAt(0);
                }
            }));
            Monitor.Exit(this);
            return l;
        }

        public void Offer(Link l)
        {
            Monitor.Enter(this);
            d.Invoke((Action)(() =>
            {
                Add(l);
            }));
            Monitor.Exit(this);
        }

        public void Push(Link l)
        {
            Monitor.Enter(this);
            d.Invoke((Action)(() =>
            {
                Insert(0, l);
            }));
            Monitor.Exit(this);
        }
    }
}
