using System;

namespace AssertEventIsReceived
{
    class Program
    {
        static void Main(string[] args)
        {
            TestTheExplicitWay();
            TestUsingReflection();
        }

        /// <summary>
        /// The explicit, copy and paste way of figuring out if a certain action raises a certain event.
        /// </summary>
        static void TestTheExplicitWay()
        {
            var didEventRaise = false;
            Action<object, EventArgs> markEventAsRaised =
                (s, e) =>
                {
                    didEventRaise = true;
                };

            var thing = new Thing();

            try
            {
                thing.SomethingHappened += new Thing.SomethingHappenedEventHandler(markEventAsRaised);
                thing.TriggerEvent();
            }
            finally
            {
                thing.SomethingHappened -= new Thing.SomethingHappenedEventHandler(markEventAsRaised);
            }

            AssertIsTrue(didEventRaise);
        }

        /// <summary>
        /// With a little bit of reflection magic we can cut down on copy-and-paste code, and therefore defects
        /// </summary>
        static void TestUsingReflection()
        {
            AssertEventIsReceived<Thing, Thing.SomethingHappenedEventHandler>("SomethingHappened", "TriggerEvent");
        }

        class Thing
        {
            public void TriggerEvent()
            {
                SomethingHappened?.Invoke(this, new EventArgs());
            }

            public event SomethingHappenedEventHandler SomethingHappened;
            public delegate void SomethingHappenedEventHandler(object sender, EventArgs e);
        }

        static void AssertEventIsReceived<TEventSender, TEventHandler>(string eventName, string triggerMethodName)
        {
            var wasEventEverRaised = false;
            Action<object, EventArgs> markEventAsRaised =
                (s, e) =>
                {
                    wasEventEverRaised = true;
                };

            var underTest = Activator.CreateInstance<TEventSender>();
            var toBind = typeof(TEventSender).GetEvent(eventName);
            var triggerMethod = typeof(TEventSender).GetMethod(triggerMethodName);
            
            // Here's the magic.
            var markEventAsRaisedWrapper = Delegate.CreateDelegate(typeof(TEventHandler), markEventAsRaised, "Invoke");

            try
            {
                toBind.AddEventHandler(underTest, markEventAsRaisedWrapper);
                triggerMethod.Invoke(underTest, null);
            }
            finally
            {
                toBind.RemoveEventHandler(underTest, markEventAsRaisedWrapper);
            }

            AssertIsTrue(wasEventEverRaised);
        }

        #region Test boilerplate

        private static void AssertIsTrue(bool v)
        {
            if (!v)
            {
                throw new Exception("Assertion failed");
            }
        }

        #endregion
    }
}
