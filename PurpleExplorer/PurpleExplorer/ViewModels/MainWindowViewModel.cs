﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Native.Interop;
using DynamicData;
using PurpleExplorer.Helpers;
using PurpleExplorer.Models;
using PurpleExplorer.Views;
using Splat;
using ReactiveUI;
using System.Threading.Tasks;

namespace PurpleExplorer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<ServiceBusResource> ConnectedServiceBuses { get; }
        public ObservableCollection<Message> Messages { get; set; }
        public ObservableCollection<Message> DlqMessages { get; }
        private IServiceBusHelper ServiceBusHelper { get; }
        private string _connectionString { get; set; }
        public string _messageTabHeader;
        public string MessagesTabHeader
        {
            get => _messageTabHeader;
            set => this.RaiseAndSetIfChanged(ref _messageTabHeader, value);
        }
        public string _dlqTabHeader;

        public string DLQTabHeader 
        {
            get => _dlqTabHeader;
            set => this.RaiseAndSetIfChanged(ref _dlqTabHeader, value);
        }

        public MainWindowViewModel(IServiceBusHelper serviceBusHelper = null)
        {
            ServiceBusHelper = serviceBusHelper ?? Locator.Current.GetService<IServiceBusHelper>();
            ConnectedServiceBuses = new ObservableCollection<ServiceBusResource>();
            Messages = new ObservableCollection<Message>();
            DlqMessages = new ObservableCollection<Message>();
            MessagesTabHeader = "Messages";
            DLQTabHeader = "Dead-letter";
        }
        private void GenerateMockMessages(int count, int dlqCount)
        {
            Random random = new Random();
            for (int i = 0; i < count; i++)
            {
                Messages.Add(new Message()
                {
                    Content = "Mocked Message " + i,
                    Size = random.Next(1, 1024)
                });
            }
            
            for (int i = 0; i < dlqCount; i++)
            {
                DlqMessages.Add(new Message()
                {
                    Content = "Mocked Message " + i,
                    Size = random.Next(1, 1024)
                });
            }
        }

        public async void BtnPopupCommand()
        {
            var viewModel = new ConnectionStringWindowViewModel();

            var returnedViewModel = await ModalWindowHelper.ShowModalWindow<ConnectionStringWindow, ConnectionStringWindowViewModel>(viewModel, 700, 100);
            _connectionString = returnedViewModel.ConnectionString;

            if (string.IsNullOrEmpty(_connectionString))
            {
                return;
            }

            try
            {
                var namespaceInfo = await ServiceBusHelper.GetNamespaceInfo(_connectionString);
                var topics = await ServiceBusHelper.GetTopics(_connectionString);

                var newResource = new ServiceBusResource
                {
                    Name = namespaceInfo.Name,
                    ConnectionString = _connectionString,
                    Topics = new ObservableCollection<ServiceBusTopic>(topics)
                };

                ConnectedServiceBuses.Add(newResource);
                //GenerateMockMessages(8, 2);
            }
            catch (ArgumentException)
            {
                await MessageBoxHelper.ShowError("The connection string is invalid.");
            }
        }

        public async void FillMessages(string subscriptionName, string topicName)
        {
            DlqMessages.Clear();
            var dlqMessages = await ServiceBusHelper.GetDlqMessages(_connectionString, subscriptionName, topicName);
            DlqMessages.AddRange(dlqMessages);
        }

        public async Task SetSubscripitonMessages(ServiceBusSubscription subscription)
        {
            var messages = await ServiceBusHelper.GetMessagesBySubscription(_connectionString, subscription.Topic.Name, subscription.Name);
            Messages.Clear();
            Messages.AddRange(messages);

            this.MessagesTabHeader = "Messages (" + messages.Count.ToString() + ")";            
        }
    }
}