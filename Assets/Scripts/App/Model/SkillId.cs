using System;

namespace Loom.ZombieBattleground.Data
{
    public interface ISkillIdOwner
    {
        SkillId SkillId { get; }
    }

    public struct SkillId : IEquatable<SkillId>
    {
        public long Id { get; }

        public SkillId(long id)
        {
            Id = id;
        }

        public bool Equals(SkillId other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SkillId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(SkillId left, SkillId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SkillId left, SkillId right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"(SkillId: {Id})";
        }
    }
}
