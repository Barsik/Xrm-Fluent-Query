﻿using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xrm.Oss.FluentQuery
{
    public static class IOrganizationServiceFluentQuery
    {
        public static IFluentQuery<T> Query<T>(this IOrganizationService service, string entityName) where T : Entity
        {
            return new FluentQuery<T>(entityName, service);
        }

        public static IFluentQuery<Entity> Query(this IOrganizationService service, string entityName)
        {
            return new FluentQuery<Entity>(entityName, service);
        }
    }

    public interface IFluentQuery<T> where T : Entity
    {
        IFluentQuery<T> IncludeColumns(params string[] columns);
        QueryExpression Expression { get; }
        List<T> Retrieve();
        List<T> RetrieveAll();

        IFluentQuerySetting<T> With { get; }

        IFluentQuery<T> Link(Action<IFluentLinkEntity> definition);
    }

    public interface IFluentQuerySetting<T> where T : Entity
    {
        IFluentQuery<T> RecordCount(int? topCount);
        IFluentQuery<T> DatabaseLock(bool useLock = true);
        IFluentQuery<T> UniqueRecords(bool unique = true);
        IFluentQuery<T> PageInfo(PagingInfo pageInfo);
        IFluentQuery<T> TotalRecordCount(bool returnTotalRecordCount = true);
    }

    public class FluentQuery<T> : IFluentQuery<T>, IFluentQuerySetting<T> where T : Entity
    {
        private QueryExpression _query;
        private IOrganizationService _service;

        public FluentQuery(string entityName, IOrganizationService service)
        {
            _query = new QueryExpression
            {
                EntityName = entityName,
                ColumnSet = new ColumnSet(),
                NoLock = true,
                PageInfo = new PagingInfo
                {
                    PageNumber = 1
                }
            };

            _service = service;
        }

        public IFluentQuery<T> IncludeColumns(params string[] columns)
        {
            _query.ColumnSet.AddColumns(columns);

            return this;
        }

        public IFluentQuerySetting<T> With
        {
            get
            {
                return this;
            }
        }

        public IFluentQuery<T> RecordCount(int? topCount)
        {
            _query.TopCount = topCount;

            return this;
        }

        public IFluentQuery<T> DatabaseLock(bool useLock = true)
        {
            _query.NoLock = !useLock;

            return this;
        }

        public List<T> Retrieve()
        {
            return _service.RetrieveMultiple(_query).Entities.Select(e => e.ToEntity<T>())
                .ToList();
        }

        public List<T> RetrieveAll()
        {
            var records = new List<T>();

            var previousPageNumber = _query.PageInfo.PageNumber;
            var previousPagingCookie = _query.PageInfo.PagingCookie;

            var moreRecords = false;
            var pageNumber = previousPageNumber;
            string pagingCookie = previousPagingCookie;

            do
            {
                _query.PageInfo.PageNumber = pageNumber;
                _query.PageInfo.PagingCookie = pagingCookie;

                var response = _service.RetrieveMultiple(_query);
                var result = response.Entities.Select(e => e.ToEntity<T>())
                    .ToList();

                records.AddRange(result);

                moreRecords = response.MoreRecords;
                pageNumber++;
            }
            while (moreRecords);

            _query.PageInfo.PageNumber = previousPageNumber;
            _query.PageInfo.PagingCookie = previousPagingCookie;

            return records;
        }

        public IFluentQuery<T> UniqueRecords(bool unique = true)
        {
            _query.Distinct = true;

            return this;
        }

        public IFluentQuery<T> PageInfo(PagingInfo pageInfo)
        {
            _query.PageInfo = pageInfo;

            return this;
        }

        public IFluentQuery<T> TotalRecordCount(bool returnTotalRecordCount = true)
        {
            _query.PageInfo.ReturnTotalRecordCount = returnTotalRecordCount;

            return this;
        }

        public IFluentQuery<T> Link(Action<IFluentLinkEntity> definition)
        {
            var link = new FluentLinkEntity();

            definition(link);

            _query.LinkEntities.Add(link.GetLinkEntity());

            return this;
        }
        
        public QueryExpression Expression
        {
            get
            {
                return _query;
            }
        }
    }

    public interface IFluentLinkEntity
    {
        IFluentLinkEntity FromEntity(string entityName);
        IFluentLinkEntity FromAttribute(string attributeName);
        IFluentLinkEntity ToEntity(string entityName);
        IFluentLinkEntity ToAttribute(string attributeName);

        IFluentLinkEntity IncludeColumns(params string[] columns);

        IFluentLinkEntitySetting With { get; }

        IFluentLinkEntity Link(Action<IFluentLinkEntity> definition);
    }

    public interface IFluentLinkEntitySetting
    {
        IFluentLinkEntity Alias(string name);
        IFluentLinkEntity LinkType(JoinOperator joinOperator);
    }

    public class FluentLinkEntity : IFluentLinkEntity, IFluentLinkEntitySetting
    {
        private LinkEntity _linkEntity;

        public FluentLinkEntity()
        {
            _linkEntity = new LinkEntity
            {
                Columns = new ColumnSet()
            };
        }

        public IFluentLinkEntitySetting With
        {
            get
            {
                return this;
            }
        }

        public IFluentLinkEntity Alias(string name)
        {
            _linkEntity.EntityAlias = name;

            return this;
        }

        public IFluentLinkEntity FromAttribute(string attributeName)
        {
            _linkEntity.LinkFromAttributeName = attributeName;

            return this;
        }

        public IFluentLinkEntity FromEntity(string entityName)
        {
            _linkEntity.LinkFromEntityName = entityName;

            return this;
        }

        public IFluentLinkEntity IncludeColumns(params string[] columns)
        {
            _linkEntity.Columns.AddColumns(columns);

            return this;
        }

        public IFluentLinkEntity Link(Action<IFluentLinkEntity> definition)
        {
            var link = new FluentLinkEntity();

            definition(link);

            _linkEntity.LinkEntities.Add(link.GetLinkEntity());

            return this;
        }

        public IFluentLinkEntity LinkType(JoinOperator joinOperator)
        {
            _linkEntity.JoinOperator = joinOperator;

            return this;
        }

        public IFluentLinkEntity ToAttribute(string attributeName)
        {
            _linkEntity.LinkToAttributeName = attributeName;

            return this;
        }

        public IFluentLinkEntity ToEntity(string entityName)
        {
            _linkEntity.LinkToEntityName = entityName;

            return this;
        }

        internal LinkEntity GetLinkEntity()
        {
            return _linkEntity;
        }
    }
}
