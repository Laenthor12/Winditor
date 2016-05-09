﻿namespace WindEditor
{
    public class TShortPropertyValue : TBasePropertyValue<short>
    {
        public override short Value
        {
            get { return m_value; }

            set
            {
                var oldValue = m_value;
                EditPropertyValueAction undoRedoEntry = new EditPropertyValueAction(
                    () => m_value = oldValue,
                    () => m_value = value,
                    () => OnPropertyChanged("Value"));
                m_undoStack.Push(undoRedoEntry);
            }
        }

        private readonly WUndoStack m_undoStack;
        private short m_value;

        public TShortPropertyValue(short defaultValue, WUndoStack undoStack)
        {
            m_value = defaultValue;
            m_undoStack = undoStack;
        }
    }
}
