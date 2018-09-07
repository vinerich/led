﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Led.ViewModels
{
    class LedEntityCRUDVM : LedEntityBaseVM
    {
        private bool _addGroup;
        private bool _creatingGroup;
        private bool _movingGroup;
        private bool _resizingGroup;
        
        private Size _moveDelta;

        private Cursor _frontCursor;
        public new Cursor FrontCursor
        {
            get => _frontCursor;
            set
            {
                if (_frontCursor != value)
                {
                    _frontCursor = value;
                    RaisePropertyChanged(nameof(FrontCursor));
                }
            }
        }

        private Cursor _backCursor;
        public new Cursor BackCursor
        {
            get => _backCursor;
            set
            {
                if (_backCursor != value)
                {
                    _backCursor = value;
                    RaisePropertyChanged(nameof(BackCursor));
                }
            }
        }

        public LedGroupPropertiesVM CurrentLedGroup { get; set; }

        public Command NewFrontImageCommand { get; set; }
        public Command NewBackImageCommand { get; set; }

        public Command AddLedGroupCommand { get; set; }
        public Command EditLedGroupCommand { get; set; }
        public Command DeleteLedGroupCommand { get; set; }

        public Command CloseWindowCommand { get; set; }

        public LedEntityCRUDVM(Model.LedEntity LedEntity)
            : base(LedEntity)
        {
            NewFrontImageCommand = new Command(OnNewFrontImmage);
            NewBackImageCommand = new Command(OnNewBackImmage);

            AddLedGroupCommand = new Command(OnAddLedGroupCommand, () => !(_addGroup || (FrontImagePath == null && BackImagePath == null)));
            EditLedGroupCommand = new Command(OnEditLedGroupCommand, () => CurrentLedGroup != null);
            DeleteLedGroupCommand = new Command(OnDeleteLedGroupCommand, () => CurrentLedGroup != null);

            FrontImageMouseDownCommand = new Command<MouseEventArgs>(OnFrontImageMouseDownCommand);
            FrontImageMouseMoveCommand = new Command<MouseEventArgs>(OnFrontImageMouseMoveCommand);

            BackImageMouseDownCommand = new Command<MouseEventArgs>(OnBackImageMouseDownCommand);
            BackImageMouseMoveCommand = new Command<MouseEventArgs>(OnBackImageMouseMoveCommand);

            ImageMouseUpCommand = new Command<MouseEventArgs>(OnImageMouseUpCommand);

            CloseWindowCommand = new Command(OnCloseWindowCommand);
        }

        private void OnNewFrontImmage()
        {
            string Path = App.Instance.IOService.OpenFileDialog();
            if (Path != "")
            {
                FrontImagePath = Path;
                BitmapImage Image = new BitmapImage(new Uri(FrontImagePath));
                LedEntity.ImageInfos[LedEntityView.Front].Size.Width = Image.PixelWidth;
                LedEntity.ImageInfos[LedEntityView.Front].Size.Height = Image.PixelHeight;
            }

            AddLedGroupCommand.RaiseCanExecuteChanged();
        }
        private void OnNewBackImmage()
        {
            string Path = App.Instance.IOService.OpenFileDialog();
            if (Path != "")
            { 
                BackImagePath = Path;
                BitmapImage Image = new BitmapImage(new Uri(FrontImagePath));
                LedEntity.ImageInfos[LedEntityView.Back].Size.Width = Image.PixelWidth;
                LedEntity.ImageInfos[LedEntityView.Back].Size.Height = Image.PixelHeight;
            }

            AddLedGroupCommand.RaiseCanExecuteChanged();
        }

        private void OnAddLedGroupCommand()
        {
            _addGroup = true;
            AddLedGroupCommand.RaiseCanExecuteChanged();
        }
        private void OnEditLedGroupCommand()
        {
            App.Instance.WindowService.ShowNewWindow(new Views.CRUDs.LedGroupCRUD(), CurrentLedGroup);
            GenerateLedVMs();
            RaisePropertyChanged("FrontLeds");
            RaisePropertyChanged("BackLeds");
        }
        private void OnDeleteLedGroupCommand()
        {
            LedGroups.Remove(CurrentLedGroup);
            CurrentLedGroup = null;

            GenerateLedVMs();
            RaisePropertyChanged("FrontLeds");
            RaisePropertyChanged("BackLeds");
            RaisePropertyChanged("BackLedGroups");
            RaisePropertyChanged("FrontLedGroups");

            EditLedGroupCommand.RaiseCanExecuteChanged();
            DeleteLedGroupCommand.RaiseCanExecuteChanged();
        }

        private void OnFrontImageMouseDownCommand(MouseEventArgs e)
        {
            if (_addGroup)
            {
                _AddLedGroup(e, LedEntityView.Front);
                RaisePropertyChanged(nameof(FrontLedGroups));
            }
            else if (!_ScanForLedGroups(e, LedGroups.FindAll(x => x.View == LedEntityView.Front)))
            {
                CurrentLedGroup = null;
                RaisePropertyChanged(nameof(CurrentLedGroup));
                EditLedGroupCommand.RaiseCanExecuteChanged();
                DeleteLedGroupCommand.RaiseCanExecuteChanged();
            }
        }
        private void OnBackImageMouseDownCommand(MouseEventArgs e)
        {
            if (_addGroup)
            {
                _AddLedGroup(e, LedEntityView.Back);
                RaisePropertyChanged(nameof(BackLedGroups));
            }
            else if (!_ScanForLedGroups(e, LedGroups.FindAll(x => x.View == LedEntityView.Back)))
            {
                CurrentLedGroup = null;
                RaisePropertyChanged(nameof(CurrentLedGroup));
                EditLedGroupCommand.RaiseCanExecuteChanged();
                DeleteLedGroupCommand.RaiseCanExecuteChanged();
            }
        }

        private void OnFrontImageMouseMoveCommand(MouseEventArgs e)
        {
            if (_creatingGroup || _resizingGroup)
            {
                _ResizeGroup(e);
                return;
            }
            else if (_movingGroup)
            {
                _MoveGroup(e);
                return;
            }

            //Bisschen Bling Bling
            FrontCursor = _ChangeCursor(e, LedGroups.FindAll(x => x.View == LedEntityView.Front));
        }
        private void OnBackImageMouseMoveCommand(MouseEventArgs e)
        {
            if (_creatingGroup || _resizingGroup)
            {
                _ResizeGroup(e);
                return;
            }
            else if (_movingGroup)
            {
                _MoveGroup(e);
                return;
            }

            //Bisschen Bling Bling
            BackCursor = _ChangeCursor(e, LedGroups.FindAll(x => x.View == LedEntityView.Back));
        }

        private void OnImageMouseUpCommand(MouseEventArgs e)
        {
            _ResetFlags();
        }

        private void OnCloseWindowCommand()
        {
            LedEntity.LedBuses = new Dictionary<byte, Model.LedBus>();

            foreach (LedGroupPropertiesVM LedGroupViewModel in LedGroups)
            {
                if (!LedEntity.LedBuses.ContainsKey(LedGroupViewModel.LedGroup.BusID))
                    LedEntity.LedBuses.Add(LedGroupViewModel.LedGroup.BusID, new Model.LedBus());

                LedEntity.LedBuses[LedGroupViewModel.LedGroup.BusID].LedGroups.Add(LedGroupViewModel.LedGroup);
            }

            foreach(Model.LedBus LedBus in LedEntity.LedBuses.Values)
            {
                LedBus.LedGroups.Sort((Model.LedGroup x, Model.LedGroup y) => x.PositionInBus > y.PositionInBus ? 1 : -1);
            }

            App.Instance.WindowService.CloseWindow(this);
        }

        private void _AddLedGroup(MouseEventArgs e, LedEntityView view)
        {
            LedGroups.Add(new LedGroupPropertiesVM()
            {
                StartPositionOnImage = e.GetPosition((IInputElement)e.Source),
                SizeOnImage = new Size(0, 0),
                View = view
            });
            CurrentLedGroup = LedGroups.Last();
            _creatingGroup = true;            
        }

        private bool _ScanForLedGroups(MouseEventArgs e, List<LedGroupPropertiesVM> ledGroups)
        {
            Point MousePosition = e.GetPosition((IInputElement)e.Source);
            foreach (var LedGroup in ledGroups)
            {
                Point Start = LedGroup.StartPositionOnImage;
                Point End = new Point(Start.X + LedGroup.SizeOnImage.Width, Start.Y + LedGroup.SizeOnImage.Height);

                if (MousePosition.X <= End.X + 5 && MousePosition.X >= End.X - 5 && MousePosition.Y <= End.Y + 5 && MousePosition.Y >= End.Y - 5)
                {
                    CurrentLedGroup = LedGroup;
                    RaisePropertyChanged(nameof(CurrentLedGroup));
                    EditLedGroupCommand.RaiseCanExecuteChanged();
                    DeleteLedGroupCommand.RaiseCanExecuteChanged();
                    _resizingGroup = true;
                    return true;
                }

                if (MousePosition.X <= End.X && MousePosition.X >= Start.X && MousePosition.Y <= End.Y && MousePosition.Y >= Start.Y)
                {
                    CurrentLedGroup = LedGroup;
                    RaisePropertyChanged(nameof(CurrentLedGroup));
                    EditLedGroupCommand.RaiseCanExecuteChanged();
                    DeleteLedGroupCommand.RaiseCanExecuteChanged();
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        _moveDelta = new Size(MousePosition.X - Start.X, MousePosition.Y - Start.Y);
                        _movingGroup = true;
                    }
                    return true;
                }
            }

            return false;
        }

        private void _ResizeGroup(MouseEventArgs e)
        {
            Point Start = CurrentLedGroup.StartPositionOnImage;
            Point MousePosition = e.GetPosition((IInputElement) e.Source);
            double deltaX = MousePosition.X - Start.X;
            double deltaY = MousePosition.Y - Start.Y;

            if (deltaX< 5)
                deltaX = 5;
            if (deltaY< 5)
                deltaY = 5;                    

            CurrentLedGroup.SizeOnImage = new Size(deltaX, deltaY);
            UpdateLedPositions(CurrentLedGroup);
        }

        private void _MoveGroup(MouseEventArgs e)
        {
            Point MousePosition = e.GetPosition((IInputElement)e.Source);
            double PositionX = MousePosition.X - _moveDelta.Width;
            double PositionY = MousePosition.Y - _moveDelta.Height;

            if (PositionX < 0)
                PositionX = 0;
            if (PositionY < 0)
                PositionY = 0;

            CurrentLedGroup.StartPositionOnImage = new Point(PositionX, PositionY);
            UpdateLedPositions(CurrentLedGroup);
        }

        private Cursor _ChangeCursor(MouseEventArgs e, List<LedGroupPropertiesVM> ledGroups)
        {
            foreach (var LedGroup in ledGroups)
            {
                Point Start = LedGroup.StartPositionOnImage;
                Point MousePosition = e.GetPosition((IInputElement)e.Source);
                Point End = new Point(Start.X + LedGroup.SizeOnImage.Width, Start.Y + LedGroup.SizeOnImage.Height);

                if (MousePosition.X <= End.X + 5 && MousePosition.X >= End.X - 5 && MousePosition.Y <= End.Y + 5 && MousePosition.Y >= End.Y - 5)
                {
                    return Cursors.SizeNWSE;
                }
                else if (MousePosition.X <= End.X && MousePosition.X >= Start.X && MousePosition.Y <= End.Y && MousePosition.Y >= Start.Y)
                {
                    return Cursors.SizeAll;
                }
            }

            return Cursors.Arrow;
        }

        private void _ResetFlags()
        {
            _addGroup = false;
            _creatingGroup = false;
            _movingGroup = false;
            _resizingGroup = false;
            AddLedGroupCommand.RaiseCanExecuteChanged();
        }
    }
}