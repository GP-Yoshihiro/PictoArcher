using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class TickListTests
    {
        sealed class Item { }

        [Test] public void Add_IsInvisibleUntilApplyPending()
        {
            var list = new TickList<Item>();
            list.Add(new Item());
            Assert.AreEqual(0, list.Count);
            list.ApplyPending();
            Assert.AreEqual(1, list.Count);
        }

        [Test] public void AddTwice_KeepsOne()
        {
            var list = new TickList<Item>();
            var a = new Item();
            list.Add(a);
            list.Add(a);
            list.ApplyPending();
            list.Add(a);
            list.ApplyPending();
            Assert.AreEqual(1, list.Count);
        }

        [Test] public void AddThenRemoveSameFrame_NeverAppears()
        {
            var list = new TickList<Item>();
            var a = new Item();
            list.Add(a);
            list.Remove(a);
            list.ApplyPending();
            Assert.AreEqual(0, list.Count);
        }

        [Test] public void Remove_IsVisibleAfterApplyPending()
        {
            var list = new TickList<Item>();
            var a = new Item();
            list.Add(a);
            list.ApplyPending();
            list.Remove(a);
            Assert.AreEqual(1, list.Count);
            list.ApplyPending();
            Assert.AreEqual(0, list.Count);
        }

        [Test] public void ChangesDuringLoop_DoNotBreakIteration()
        {
            var list = new TickList<Item>();
            var a = new Item();
            var b = new Item();
            list.Add(a);
            list.Add(b);
            list.ApplyPending();
            int visited = 0;
            for (int i = 0; i < list.Count; i++)
            {
                visited++;
                list.Remove(list[i]);
                list.Add(new Item());
            }
            Assert.AreEqual(2, visited);
            list.ApplyPending();
            Assert.AreEqual(2, list.Count);
        }

        [Test] public void RemoveUnknown_DoesNothing()
        {
            var list = new TickList<Item>();
            list.Add(new Item());
            list.ApplyPending();
            list.Remove(new Item());
            list.ApplyPending();
            Assert.AreEqual(1, list.Count);
        }

        [Test] public void Clear_DropsItemsAndPending()
        {
            var list = new TickList<Item>();
            list.Add(new Item());
            list.ApplyPending();
            list.Add(new Item());
            list.Clear();
            Assert.AreEqual(0, list.Count);
            list.ApplyPending();
            Assert.AreEqual(0, list.Count);
        }
    }
}
