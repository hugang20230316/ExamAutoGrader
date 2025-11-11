// Domain/Common/ValueObject.cs
namespace ExamAutoGrader.Domain.Common
{
    /// <summary>
    /// 值对象基类
    /// 提供值对象的相等性比较基础实现
    /// </summary>
    public abstract class ValueObject
    {
        /// <summary>
        /// 获取相等性比较的组件
        /// 子类需要重写此方法返回参与比较的属性值
        /// </summary>
        protected abstract IEnumerable<object> GetEqualityComponents();

        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            var other = (ValueObject)obj;
            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x?.GetHashCode() ?? 0)
                .Aggregate((x, y) => x ^ y);
        }

        public static bool operator ==(ValueObject? left, ValueObject? right)
        {
            if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
                return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;
            return left.Equals(right);
        }

        public static bool operator !=(ValueObject? left, ValueObject? right)
        {
            return !(left == right);
        }
    }
}