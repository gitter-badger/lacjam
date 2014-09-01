using System;
using Lacjam.Core.Infrastructure;
using Lacjam.Framework.Commands;

namespace Lacjam.Core.Domain.MetadataDefinitionGroups.Commands
{

    public class CreateMetadataDefinitionGroupCommand : ICommand
    {
        public CreateMetadataDefinitionGroupCommand(Guid identity, string name, string description, TrackingBase tracking) : this(identity, name, description,tracking, new MetadataBag())
        {
        }

        public CreateMetadataDefinitionGroupCommand(Guid identity, string name, string description, TrackingBase tracking, MetadataBag bag)
        {
            Identity = identity;
            Name = name;
            Description = description;
            Tracking = tracking;
            Bag = bag;
        }

        public Guid Identity{ get; private set; }
        public string Name{ get; private set; }
        public string Description{ get; private set; }
        public TrackingBase Tracking { get; private set; }
        public MetadataBag Bag { get; private set; }
    }
}
