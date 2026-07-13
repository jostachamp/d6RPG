using System;
using System.Collections.Generic;

namespace ArkhamHorror.Mechanics.Scenes
{
    public interface IDecisionProvider
    {
        T Choose<T>(DecisionRequest<T> request);
    }

    public sealed class DecisionRequest<T>
    {
        private readonly IReadOnlyList<T> choices;

        public DecisionRequest(string id, IEnumerable<T> choices)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A stable decision ID is required.", nameof(id));
            }

            if (choices == null)
            {
                throw new ArgumentNullException(nameof(choices));
            }

            var copy = new List<T>(choices);
            if (copy.Count == 0)
            {
                throw new ArgumentException("A decision requires at least one legal choice.", nameof(choices));
            }

            Id = id;
            this.choices = copy.AsReadOnly();
        }

        public string Id { get; }

        public IReadOnlyList<T> Choices => choices;

        public T Validate(T choice)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int index = 0; index < choices.Count; index++)
            {
                if (comparer.Equals(choices[index], choice))
                {
                    return choice;
                }
            }

            throw new InvalidOperationException("The supplied choice is not legal for this decision.");
        }
    }
}
