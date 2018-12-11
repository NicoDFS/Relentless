using JetBrains.Annotations;
using Loom.ZombieBattleground.Data;

namespace Loom.ZombieBattleground
{
    public class WorkingCard
    {
        private CardsController _cardsController;

        public Player Owner;

        public IReadOnlyCard LibraryCard;

        public CardInstanceSpecificData InstanceCard;

        public InstanceId InstanceId { get; set; }

        public WorkingCard(IReadOnlyCard cardPrototype, IReadOnlyCard card, Player player, InstanceId? id = null)
            : this(cardPrototype, new CardInstanceSpecificData(card), player, id)
        {
        }

        public WorkingCard(IReadOnlyCard cardPrototype, CardInstanceSpecificData cardInstanceData, Player player, InstanceId? id = null)
        {
            Owner = player;
            LibraryCard = new Card(cardPrototype);
            InstanceCard = cardInstanceData;

            _cardsController = GameClient.Get<IGameplayManager>().GetController<CardsController>();

            if (id == null)
            {
                InstanceId = _cardsController.GetNewCardInstanceId();
            }
            else
            {
                InstanceId = id.Value;

                if (InstanceId.Id > _cardsController.GetCardInstanceId().Id)
                {
                    _cardsController.SetNewCardInstanceId(InstanceId.Id);
                }
            }
        }

        public override string ToString()
        {
            return $"{{InstanceId: {InstanceId}, Name: {LibraryCard.Name}}}";
        }
    }

}
