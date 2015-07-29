﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Prism.API.Behaviours;
using Prism.API.Defs;
using Terraria;
using Terraria.ID;

namespace Prism.Mods.DefHandlers
{
    /// <summary>
    /// Handles all the entity defs.
    /// </summary>
    abstract class EntityDefHandler<TEntityDef, TBehaviour, TEntity>
        where TEntity : class
        where TBehaviour : EntityBehaviour<TEntity>
        where TEntityDef : EntityDef<TBehaviour, TEntity>
    {
        /// <summary>
        /// The entity type index to which the next entity added will be assigned;
        /// </summary>
        internal int NextTypeIndex;

        public Dictionary<int   , TEntityDef> DefsByType        = new Dictionary<int   , TEntityDef>();
        public Dictionary<string, TEntityDef> VanillaDefsByName = new Dictionary<string, TEntityDef>();

        int?
            minVanillaId = null,
            maxVanillaId = null;
        FieldInfo[] idFields = null;
        int[] idValues = null;
        string[] idNames = null;

        internal FieldInfo[] IDFields
        {
            get
            {
                if (idFields == null)
                    idFields = IDContainerType.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType.IsPrimitive && f.FieldType != typeof(bool) && f.Name != "Count").ToArray();

                return idFields;
            }
        }
        internal int[] IDValues
        {
            get
            {
                if (idValues == null)
                    idValues = IDFields.Select(f => (int)f.GetValue(null)).ToArray();

                return idValues;
            }
        }
        internal string[] IDNames
        {
            get
            {
                if (idNames == null)
                    idNames = IDFields.Select(f => f.Name).ToArray();

                return idNames;
            }
        }

        protected int MinVanillaID
        {
            get
            {
                if (minVanillaId == null)
                    minVanillaId = IDFields.Select(f => (int)f.GetValue(null)).Min();

                return minVanillaId.Value;
            }
        }
        protected int MaxVanillaID
        {
            get
            {
                if (maxVanillaId == null)
                    maxVanillaId = IDFields.Select(f => (int)f.GetValue(null)).Max();

                return maxVanillaId.Value;
            }
        }

        protected abstract Type IDContainerType
        {
            get;
        }

        protected EntityDefHandler()
        {
            Reset();
        }

        protected abstract void ExtendVanillaArrays(int amt = 1);

        protected abstract TEntity GetVanillaEntityFromID(int id);

        protected abstract void CopyEntityToDef(TEntity entity, TEntityDef def);
        protected abstract void CopyDefToEntity(TEntityDef def, TEntity entity);

        protected abstract List<LoaderError> CheckTextures(TEntityDef def);
        protected abstract List<LoaderError> LoadTextures (TEntityDef def);
        protected abstract void LoadSetProperties(TEntityDef def);

        protected abstract TEntityDef CreateEmptyDefWithDisplayName(TEntity entity);
        protected abstract int GetRegularType(TEntity entity);

        protected virtual void PostFillVanilla() { }

        internal void FillVanilla()
        {
            for (int id = MinVanillaID; id < MaxVanillaID; id++)
            {
                if (id == 0)
                    continue;

                TEntity entity = GetVanillaEntityFromID(id);
                TEntityDef def = CreateEmptyDefWithDisplayName(entity);

                def.InternalName = IDNames[Array.IndexOf(IDValues, GetRegularType(entity))];
                if (String.IsNullOrEmpty(def.InternalName))
                    continue;

                CopyEntityToDef(entity, def);

                DefsByType.Add(id, def);
                VanillaDefsByName.Add(def.InternalName, def);
            }

            PostFillVanilla();
        }

        internal void Reset()
        {
            ExtendVanillaArrays(-1);

            NextTypeIndex = MaxVanillaID;

            DefsByType.Clear();
        }

        internal IEnumerable<LoaderError> Load(Dictionary<string, TEntityDef> dict)
        {
            var err = new List<LoaderError>();

            ExtendVanillaArrays(dict.Count);

            foreach (var def in dict.Values)
            {
                if (!Main.dedServ)
                {
                    var cterrs = CheckTextures(def);

                    if (cterrs.Count > 0)
                    {
                        err.AddRange(cterrs);
                        continue;
                    }
                }

                def.Type = NextTypeIndex;

                if (!Main.dedServ)
                {
                    var lterrs = LoadTextures(def);

                    if (lterrs.Count > 0)
                    {
                        err.AddRange(lterrs);
                        continue;
                    }
                }

                LoadSetProperties(def);
                DefsByType.Add(NextTypeIndex++, def);
            }

            return err;
        }
    }
}