using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DSharpPlus.CommandsNext.Entities;

namespace DSharpPlus.CommandsNext.Builders
{
    /// <summary>
    /// Represents an interface to build a command group.
    /// </summary>
    public sealed class CommandGroupBuilder : CommandBuilder
    {
        /// <summary>
        /// Gets the list of child commands registered for this group.
        /// </summary>
        public IReadOnlyList<CommandBuilder> Children { get; }
        private List<CommandBuilder> ChildrenList { get; }

        /// <summary>
        /// Creates a new module-less command group builder.
        /// </summary>
        public CommandGroupBuilder()
            : this(null)
        { }

        /// <summary>
        /// Creates a new command group builder.
        /// </summary>
        /// <param name="module">Module on which this group is to be defined.</param>
        public CommandGroupBuilder(ICommandModule module) 
            : base(module)
        {
            ChildrenList = new List<CommandBuilder>();
            Children = new ReadOnlyCollection<CommandBuilder>(ChildrenList);
        }

        /// <summary>
        /// Adds a command to the collection of child commands for this group.
        /// </summary>
        /// <param name="child">Command to add to the collection of child commands for this group.</param>
        /// <returns>This builder.</returns>
        public CommandGroupBuilder WithChild(CommandBuilder child)
        {
            ChildrenList.Add(child);
            return this;
        }

        internal override Command Build(CommandGroup parent)
        {
            var cmd = new CommandGroup
            {
                Name = Name,
                Description = Description,
                Aliases = Aliases,
                ExecutionChecks = ExecutionChecks,
                IsHidden = IsHidden,
                Parent = parent,
                Overloads = new ReadOnlyCollection<CommandOverload>(Overloads.Select(xo => xo.Build()).ToList()),
                Module = Module,
                CustomAttributes = CustomAttributes
            };

            var cs = new List<Command>();
            foreach (var xc in Children)
                cs.Add(xc.Build(cmd));

            cmd.Children = new ReadOnlyCollection<Command>(cs);
            return cmd;
        }
    }
}
