using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Unicord.Universal.Models.User;

namespace Unicord.Universal.Models.Relationships
{
    public class RelationshipViewModel : ViewModelBase, IEquatable<RelationshipViewModel>, IComparable<RelationshipViewModel>
    {
        private DiscordRelationship rel;

        public RelationshipViewModel(DiscordRelationship rel, ViewModelBase owner)
            : base(owner)
        {
            this.rel = rel;
        }

        public ulong Id 
            => rel.Id;

        public UserViewModel User
            => new UserViewModel(rel.User, null, this);

        public int CompareTo(RelationshipViewModel other)
        {
            throw new NotImplementedException();
        }

        public bool Equals(RelationshipViewModel other)
        {
            return rel.Id == other.rel.Id;
        }
    }
}
