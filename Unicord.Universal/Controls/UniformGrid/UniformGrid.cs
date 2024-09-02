// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;

namespace Unicord.Universal.Controls
{
    /// <summary>
    /// The UniformGrid control presents information within a Grid with even spacing.
    /// </summary>
    public partial class UniformGrid : Grid
    {
        // Internal list we use to keep track of items that we don't have space to layout.
        private List<UIElement> _overflow = new List<UIElement>();

        /// <summary>
        /// The <see cref="TakenSpotsReferenceHolder"/> instance in use, if any.
        /// </summary>
        private TakenSpotsReferenceHolder _spotref;

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            // Get all Visible FrameworkElement Children
            var visible = Children.Where(item => item.Visibility != Visibility.Collapsed && item is FrameworkElement).Select(item => item as FrameworkElement).ToArray();

#pragma warning disable SA1008 // Opening parenthesis must be spaced correctly
            var (rows, columns) = GetDimensions(visible, Rows, Columns, FirstColumn);
#pragma warning restore SA1008 // Opening parenthesis must be spaced correctly

            // Now that we know size, setup automatic rows/columns
            // to utilize Grid for UniformGrid behavior.
            // We also interleave any specified rows/columns with fixed sizes.
            SetupRowDefinitions(rows);
            SetupColumnDefinitions(columns);

            TakenSpotsReferenceHolder spotref;

            // If the last spot holder matches the size currently in use, just reset
            // that instance and reuse it to avoid allocating a new bit array.
            if (_spotref != null && _spotref.Height == rows && _spotref.Width == columns)
            {
                spotref = _spotref;

                spotref.Reset();
            }
            else
            {
                spotref = _spotref = new TakenSpotsReferenceHolder(rows, columns);
            }

            // Figure out which children we should automatically layout and where available openings are.
            foreach (var child in visible)
            {
                var row = GetRow(child);
                var col = GetColumn(child);
                var rowspan = GetRowSpan(child);
                var colspan = GetColumnSpan(child);

                // If an element needs to be forced in the 0, 0 position,
                // they should manually set UniformGrid.AutoLayout to False for that element.
                if ((row == 0 && col == 0 && GetAutoLayout(child) == null) ||
                    GetAutoLayout(child) == true)
                {
                    SetAutoLayout(child, true);
                }
                else
                {
                    SetAutoLayout(child, false);

                    spotref.Fill(true, row, col, colspan, rowspan);
                }
            }

            // Setup available size with our known dimensions now.
            // UniformGrid expands size based on largest singular item.
            double columnSpacingSize = 0;
            double rowSpacingSize = 0;

            columnSpacingSize = ColumnSpacing * (columns - 1);
            rowSpacingSize = RowSpacing * (rows - 1);

            Size childSize = new Size(
                (availableSize.Width - columnSpacingSize) / columns,
                (availableSize.Height - rowSpacingSize) / rows);

            double maxWidth = 0.0;
            double maxHeight = 0.0;

            var currentRow = -1;
            var currentHeight = 0.0;

            // Set Grid Row/Col for every child with autolayout = true
            // Backwards with FlowDirection
            var freespots = GetFreeSpot(spotref, FirstColumn, Orientation == Orientation.Vertical).GetEnumerator();
            foreach (var child in visible)
            {
                // Set location if we're in charge
                if (GetAutoLayout(child) == true)
                {
                    if (freespots.MoveNext())
                    {
                        var (row, column) = freespots.Current;
                        SetRow(child, row);
                        SetColumn(child, column);

                        var rowspan = GetRowSpan(child);
                        var colspan = GetColumnSpan(child);

                        if (rowspan > 1 || colspan > 1)
                        {
                            // TODO: Need to tie this into iterator
                            spotref.Fill(true, row, column, colspan, rowspan);
                        }
                    }
                    else
                    {
                        // We've run out of spots as the developer has
                        // most likely given us a fixed size and too many elements
                        // Therefore, tell this element it has no size and move on.
                        child.Measure(Size.Empty);

                        _overflow.Add(child);

                        continue;
                    }
                }
                else if (GetRow(child) < 0 || GetRow(child) >= rows ||
                         GetColumn(child) < 0 || GetColumn(child) >= columns)
                {
                    // A child is specifying a location, but that location is outside
                    // of our grid space, so we should hide it instead.
                    child.Measure(Size.Empty);

                    _overflow.Add(child);

                    continue;
                }

                // Get measurement for max child
                child.Measure(childSize);

                maxWidth = Math.Max(child.DesiredSize.Width, maxWidth);

                var childRow = GetRow(child);
                if (currentRow != childRow)
                {
                    maxHeight += currentHeight;
                    currentHeight = 0;
                    currentRow = childRow;
                }

                currentHeight = Math.Max(child.DesiredSize.Height, currentHeight);
            }

            // Return our desired size based on the largest child we found, our dimensions, and spacing.
            var desiredSize = new Size((maxWidth * columns) + columnSpacingSize, maxHeight + currentHeight + rowSpacingSize);
            var measuredSize = base.MeasureOverride(desiredSize);
            return new Size(desiredSize.Width, measuredSize.Height);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // Have grid to the bulk of our heavy lifting.
            var size = base.ArrangeOverride(finalSize);

            // Make sure all overflown elements have no size.
            foreach (var child in _overflow)
            {
                child.Arrange(default);
            }

            _overflow = new List<UIElement>(); // Reset for next time.

            return size;
        }
    }
}