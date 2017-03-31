using System;
using System.Reflection;

namespace AssertEventIsReceived
{
    class Program
    {

        static void ExplicitWay()
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

        private static void AssertIsTrue(bool v)
        {
            if (!v)
            {
                throw new Exception("Assertion failed");
            }
        }

        static void Main(string[] args)
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
    }

    /*
     *
    Private Sub AssertEventIsReceived(Of TEventWrapper, TEventHandler)(eventName As String, triggerMethod As String)

        Dim wrapper = Activator.CreateInstance(Of TEventWrapper)()
        Dim gotEvent = False

        Dim handler =
            Sub(sender As Object, e As EventArgs)
                gotEvent = True
            End Sub

        ' Create a wrapper for our anonymous delegate in the type that the event subscription expects.
        Dim combinedDelegate = [Delegate].CreateDelegate(GetType(TEventHandler),
                                                         handler, NameOf(handler.Invoke)) ' Invoke isn't a public method on Delegate, but it is on the closure

        Dim targetEvent = GetType(TEventWrapper).GetEvent(eventName)
        If IsNothing(targetEvent) Then
            Assert.Fail($"Could not find the event '{eventName}' on the trigger wrapper type '{GetType(TEventWrapper).FullName}'")
        End If

        Dim wrapperTriggerMethod = GetType(TEventWrapper).GetMethod(triggerMethod)
        If IsNothing(wrapperTriggerMethod) Then
            Assert.Fail($"Could not find the trigger method '{triggerMethod}' on the trigger wrapper type '{GetType(TEventWrapper).FullName}'")
        End If

        Try
            targetEvent.AddEventHandler(wrapper, combinedDelegate)
            wrapperTriggerMethod.Invoke(wrapper, Nothing)
        Finally
            targetEvent.RemoveEventHandler(wrapper, combinedDelegate)
        End Try

        Assert.IsTrue(gotEvent,
                      $"Expected the event '{eventName}' to be triggered when the trigger method '{triggerMethod}' was called on the trigger wrapper type '{GetType(TEventWrapper).FullName}', but it was not.")
    End Sub
    */
}
