﻿using Fluxor;
using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.Common.RazorLib.Notifications.States;

namespace Luthetus.Common.RazorLib.Notifications.Models;

public class NotificationService : INotificationService
{
    private readonly IDispatcher _dispatcher;

    public NotificationService(
        IDispatcher dispatcher,
        IState<NotificationState> notificationState)
    {
        _dispatcher = dispatcher;
        NotificationStateWrap = notificationState;
    }

    public IState<NotificationState> NotificationStateWrap { get; }

    public void RegisterNotificationRecord(NotificationRecord notificationRecord)
    {
        _dispatcher.Dispatch(new NotificationState.RegisterAction(notificationRecord));
    }

    public void DisposeNotificationRecord(Key<NotificationRecord> notificationKey)
    {
        _dispatcher.Dispatch(new NotificationState.DisposeAction(notificationKey));
    }
}