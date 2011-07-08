﻿/*
Copyright (c) 2011 <a href="http://www.gutgames.com">James Craig</a>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.*/

#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Utilities.ORM.QueryProviders.Interfaces;
using Utilities.ORM.Mapping.Interfaces;
using Utilities.DataTypes;
using Utilities.SQL.MicroORM;
using System.Data;
#endregion

namespace Utilities.ORM.QueryProviders
{
    /// <summary>
    /// Default query provider
    /// </summary>
    public class Default
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="AssemblyUsing">Assembly using</param>
        public Default(Assembly AssemblyUsing)
        {
            Setup(AssemblyUsing);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="AssembliesUsing">Assemblies using</param>
        public Default(Assembly[] AssembliesUsing)
        {
            foreach (Assembly Assembly in AssembliesUsing)
            {
                Setup(Assembly);
            }
        }

        #endregion

        #region Functions

        #region Setup

        /// <summary>
        /// Sets up the system
        /// </summary>
        /// <param name="AssemblyUsing">Assembly to set up</param>
        private void Setup(Assembly AssemblyUsing)
        {
            if (Databases == null)
                Databases = new System.Collections.Generic.List<IDatabase>();
            System.Collections.Generic.List<Type> Types = Utilities.Reflection.Reflection.GetTypes(AssemblyUsing, typeof(IDatabase));
            foreach (Type Type in Types)
            {
                Type BaseType = Type.BaseType;
                IDatabase TempObject = (IDatabase)Activator.CreateInstance(Type);
                if (!string.IsNullOrEmpty(TempObject.ConnectionString))
                {
                    MicroORM.Database(TempObject.ConnectionString, TempObject.Name);
                    Databases.Add(TempObject);
                }
            }
        }

        #endregion

        #region AddMapping

        /// <summary>
        /// Adds a mapping
        /// </summary>
        /// <param name="Mapping">Mapping to add</param>
        public virtual void AddMapping(IMapping Mapping)
        {
            if (Mappings == null)
                Mappings = new ListMapping<IDatabase, IMapping>();
            IDatabase Database = Databases.First(x => Utilities.Reflection.Reflection.IsOfType(x, Mapping.DatabaseConfigType)
                                                        && !string.IsNullOrEmpty(x.ConnectionString));
            Mapping.AddToQueryProvider(Database);
            Mappings.Add(Database, Mapping);
        }

        #endregion

        #region Any

        /// <summary>
        /// Returns any item that matches the criteria
        /// </summary>
        /// <typeparam name="ObjectType">Object type</typeparam>
        /// <param name="Parameters">Parameters used in the where clause</param>
        /// <returns>First item matching the criteria</returns>
        public virtual ObjectType Any<ObjectType>(params IParameter[] Parameters) where ObjectType : class,new()
        {
            ObjectType ReturnVal = new ObjectType();
            foreach (IDatabase Database in Mappings.Keys)
            {
                IMapping Mapping = Mappings[Database].First(x => x.ObjectType == typeof(ObjectType));
                if (Mapping != null)
                {
                    if (Mapping.AnyCommand == null)
                        return Any<ObjectType>("*", Parameters);
                    using (MicroORM ORMObject = new MicroORM(Database.Name))
                    {
                        ReturnVal = ORMObject.Map<ObjectType>().Any(Mapping.AnyCommand.CommandToRun, Mapping.AnyCommand.CommandType, ReturnVal, Parameters);
                    }
                }
            }
            return ReturnVal;
        }

        /// <summary>
        /// Returns any item that matches the criteria
        /// </summary>
        /// <typeparam name="ObjectType">Object type</typeparam>
        /// <param name="Columns">Columns to load</param>
        /// <param name="Parameters">Parameters used in the where clause</param>
        /// <returns>First item that matches the criteria</returns>
        public virtual ObjectType Any<ObjectType>(string Columns, params IParameter[] Parameters) where ObjectType : class,new()
        {
            ObjectType ReturnVal = new ObjectType();
            foreach (IDatabase Database in Mappings.Keys)
            {
                if (Mappings[Database].First(x => x.ObjectType == typeof(ObjectType)) != null)
                {
                    using (MicroORM ORMObject = new MicroORM(Database.Name))
                    {
                        ReturnVal = ORMObject.Map<ObjectType>().Any(Columns, ReturnVal, Parameters);
                    }
                }
            }
            return ReturnVal;
        }

        /// <summary>
        /// Returns any item that matches the criteria
        /// </summary>
        /// <typeparam name="ObjectType">Object type</typeparam>
        /// <param name="Command">Command to run</param>
        /// <param name="CommandType">Command type</param>
        /// <param name="Parameters">Parameters used in the where clause</param>
        /// <returns>First item that matches the criteria</returns>
        public virtual ObjectType Any<ObjectType>(string Command, CommandType CommandType, params IParameter[] Parameters) where ObjectType : class,new()
        {
            ObjectType ReturnVal = new ObjectType();
            foreach (IDatabase Database in Mappings.Keys)
            {
                if (Mappings[Database].First(x => x.ObjectType == typeof(ObjectType)) != null)
                {
                    using (MicroORM ORMObject = new MicroORM(Database.Name))
                    {
                        ReturnVal = ORMObject.Map<ObjectType>().Any(Command, CommandType, ReturnVal, Parameters);
                    }
                }
            }
            return ReturnVal;
        }

        #endregion

        #region All

        /// <summary>
        /// Returns a list of objects that meet the criteria
        /// </summary>
        /// <typeparam name="ObjectType">Object type</typeparam>
        /// <param name="Columns">Columns to load</param>
        /// <param name="Limit">Limit on the number of items to return</param>
        /// <param name="OrderBy">Order by clause (minus the ORDER BY)</param>
        /// <param name="Parameters">Parameters used in the where clause</param>
        /// <returns>A list of objects that meet the criteria</returns>
        public virtual IEnumerable<ObjectType> All<ObjectType>(string Columns, int Limit, string OrderBy, params IParameter[] Parameters) where ObjectType : class,new()
        {
            System.Collections.Generic.List<ObjectType> ReturnValues = new System.Collections.Generic.List<ObjectType>();
            foreach (IDatabase Database in Mappings.Keys)
            {
                if (Mappings[Database].First(x => x.ObjectType == typeof(ObjectType)) != null)
                {
                    using (MicroORM ORMObject = new MicroORM(Database.Name))
                    {
                        ReturnValues = (System.Collections.Generic.List<ObjectType>)ORMObject.Map<ObjectType>().All(Columns, Limit, OrderBy, ReturnValues, Parameters);
                    }
                }
            }
            return ReturnValues;
        }

        /// <summary>
        /// Returns a list of objects that meet the criteria
        /// </summary>
        /// <typeparam name="ObjectType">Object type</typeparam>
        /// <param name="Command">Command to run</param>
        /// <param name="CommandType">Command type</param>
        /// <param name="Parameters">Parameters used in the where clause</param>
        /// <returns>A list of objects that meet the criteria</returns>
        public virtual IEnumerable<ObjectType> All<ObjectType>(string Command, CommandType CommandType, params IParameter[] Parameters) where ObjectType : class,new()
        {
            System.Collections.Generic.List<ObjectType> ReturnValues = new System.Collections.Generic.List<ObjectType>();
            foreach (IDatabase Database in Mappings.Keys)
            {
                if (Mappings[Database].First(x => x.ObjectType == typeof(ObjectType)) != null)
                {
                    using (MicroORM ORMObject = new MicroORM(Database.Name))
                    {
                        ReturnValues = (System.Collections.Generic.List<ObjectType>)ORMObject.Map<ObjectType>().All(Command, CommandType, ReturnValues, Parameters);
                    }
                }
            }
            return ReturnValues;
        }

        /// <summary>
        /// Returns a list of objects that meet the criteria
        /// </summary>
        /// <typeparam name="ObjectType">Object type</typeparam>
        /// <param name="Parameters">Parameters used in the where clause</param>
        /// <returns>A list of objects that meet the criteria</returns>
        public virtual IEnumerable<ObjectType> All<ObjectType>(params IParameter[] Parameters) where ObjectType : class,new()
        {
            System.Collections.Generic.List<ObjectType> ReturnValues = new System.Collections.Generic.List<ObjectType>();
            foreach (IDatabase Database in Mappings.Keys)
            {
                IMapping Mapping = Mappings[Database].First(x => x.ObjectType == typeof(ObjectType));
                if (Mapping != null)
                {
                    if (Mapping.AllCommand == null)
                        return All<ObjectType>("*", 0, "", Parameters);
                    using (MicroORM ORMObject = new MicroORM(Database.Name))
                    {
                        ReturnValues = (System.Collections.Generic.List<ObjectType>)ORMObject.Map<ObjectType>().All(Mapping.AllCommand.CommandToRun, Mapping.AllCommand.CommandType, ReturnValues, Parameters);
                    }
                }
            }
            return ReturnValues;
        }

        #endregion

        #region Paged

        /// <summary>
        /// Returns a paged list of items
        /// </summary>
        /// <typeparam name="ObjectType">Object type</typeparam>
        /// <param name="Columns">Columns to load</param>
        /// <param name="OrderBy">Order by clause (minus the ORDER BY part)</param>
        /// <param name="PageSize">Page size</param>
        /// <param name="CurrentPage">Current page (starting with 0)</param>
        /// <param name="Parameters">Parameters used in the where clause</param>
        /// <returns>A paged list of items that match the criteria</returns>
        public virtual IEnumerable<ObjectType> Paged<ObjectType>(string Columns = "*", string OrderBy = "", int PageSize = 25, int CurrentPage = 0, params IParameter[] Parameters) where ObjectType : class,new()
        {
            System.Collections.Generic.List<ObjectType> ReturnValues = new System.Collections.Generic.List<ObjectType>();
            foreach (IDatabase Database in Mappings.Keys)
            {
                IMapping Mapping = Mappings[Database].First(x => x.ObjectType == typeof(ObjectType));
                if (Mapping != null)
                {
                    using (MicroORM ORMObject = new MicroORM(Database.Name))
                    {
                        ReturnValues = (System.Collections.Generic.List<ObjectType>)ORMObject.Map<ObjectType>().Paged(Columns, OrderBy, PageSize, CurrentPage, ReturnValues, Parameters);
                    }
                }
            }
            return ReturnValues;
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Mappings associated to databases
        /// </summary>
        public virtual ListMapping<IDatabase, IMapping> Mappings { get; set; }

        /// <summary>
        /// List of database configurations
        /// </summary>
        public virtual System.Collections.Generic.List<IDatabase> Databases { get; set; }

        #endregion
    }
}