using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Enums;
using Unicord.Universal.Models.User;

namespace Unicord.Universal.Models.Relationships
{
    public class RelationshipViewModel(DiscordRelationship rel, ViewModelBase owner) : ViewModelBase(owner), IEquatable<RelationshipViewModel>, IComparable<RelationshipViewModel>
    {
        private DiscordRelationship rel = rel;

        public ulong Id
            => rel.Id;
        public DiscordRelationshipType Type
            => rel.RelationshipType;
        public UserViewModel User
            => new UserViewModel(rel.User, null, this);

        public int CompareTo(RelationshipViewModel other)
        {
            var name1 = User?.DisplayName;
            var name2 = other?.User?.DisplayName;

            return name1?.CompareTo(name2 ?? "") ?? 0;
        }

        public bool Equals(RelationshipViewModel other)
        {
            return rel.Id == other.rel.Id;
        }
    }
}
