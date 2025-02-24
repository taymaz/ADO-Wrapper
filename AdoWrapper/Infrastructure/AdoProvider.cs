﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
//using System.Data.SqlClient;
using System.Threading.Tasks;
using AdoWrapper.Contracts;
using AdoWrapper.Extensions;
using AdoWrapper.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AdoWrapper.Infrastructure
{
    internal class AdoProvider : IAdoProvider
    {
        private readonly IOptions<ConnectionStringModel> _connectionStringModel;

        public AdoProvider(IOptions<ConnectionStringModel> connectionStringModel)
        {
            _connectionStringModel = connectionStringModel;
        }

        public T GetFirstOrDefault<T>(string sql) where T : class, new()
        {
            var result = default(T);
            var properties = TypeExtensions.GetWritableProperties<T>();

            using SqlConnection connection = new SqlConnection(_connectionStringModel.Value.ConnectionString);
            using SqlCommand command = new SqlCommand(sql, connection);

            connection.Open();

            using SqlDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection);

            var listProperties = TypeExtensions.GetListProperties<T>();

            //The original version of this method runs to the last record and returns the last item, in fact, LastOrDefault

            bool anyRecordReaded = false;

            while (reader.Read() && !anyRecordReaded )
            {
                result = new T();
                foreach (var property in properties)
                {

                    if (property.IsClassProperty())
                    {
                        if (property.IsPropertyGenericList())
                        {
                            var innerProperty = property.GetGenericArgument();

                            var obj = Activator.CreateInstance(innerProperty);

                            var list = listProperties.FirstOrDefault(c => c.GetType() == property.PropertyType);

                            var objProperties = obj.GetWritableProperties();

                            foreach (var prop in objProperties)
                            {
                                var val = reader.GetFieldValue<object>(reader.GetOrdinal(prop.Name));
                                prop.SetValue(obj, val);
                            }

                            list.Add(obj);

                            property.SetValue(result, list);

                        }
                        else
                        {
                            var obj = Activator.CreateInstance(property.PropertyType);

                            var objProperties = obj.GetWritableProperties();

                            foreach (var prop in objProperties)
                            {
                                var val = reader.GetFieldValue<object>(reader.GetOrdinal(prop.Name));
                                prop.SetValue(obj, val);
                            }

                            property.SetValue(result, obj);
                        }

                        continue;
                    }
                    var value = reader.GetValue(reader.GetOrdinal(property.Name));
                    property.SetValue(result, value);
                }
                anyRecordReaded = true;
            }

            return result;

          //  return this.GetFirstOrDefaultAsync<T>(sql).Result;
        }

        public async Task<T> GetFirstOrDefaultAsync<T>(string sql) where T : class, new()
        {
            var result = default(T);
            var properties = TypeExtensions.GetWritableProperties<T>();

            await using SqlConnection connection = new SqlConnection(_connectionStringModel.Value.ConnectionString);
            await using SqlCommand command = new SqlCommand(sql, connection);

            await connection.OpenAsync().ConfigureAwait(false);

            await using SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection).ConfigureAwait(false);

            var listProperties = TypeExtensions.GetListProperties<T>();
            //The original version of this method runs to the last record and returns the last item, in fact, LastOrDefault

            bool anyRecordReaded = false;
            while (await reader.ReadAsync().ConfigureAwait(false) && !anyRecordReaded)
            {
                result = new T();

                foreach (var property in properties)
                {
                    if (property.IsClassProperty())
                    {
                        if (property.IsPropertyGenericList())
                        {
                            var innerProperty = property.GetGenericArgument();

                            var obj = Activator.CreateInstance(innerProperty);

                            var list = listProperties.FirstOrDefault(c => c.GetType() == property.PropertyType);

                            var objProperties = obj.GetWritableProperties();

                            foreach (var prop in objProperties)
                            {
                                var val = await reader.GetFieldValueAsync<object>(reader.GetOrdinal(prop.Name));
                                prop.SetValue(obj, val);
                            }

                            list.Add(obj);

                            property.SetValue(result, list);

                        }

                        else
                        {
                            var obj = Activator.CreateInstance(property.PropertyType);

                            var objProperties = obj.GetWritableProperties();

                            foreach (var prop in objProperties)
                            {
                                var val = await reader.GetFieldValueAsync<object>(reader.GetOrdinal(prop.Name));
                                prop.SetValue(obj, val);
                            }

                            property.SetValue(result, obj);
                        }

                        continue;
                    }

                    var value = await reader.GetFieldValueAsync<object>(reader.GetOrdinal(property.Name)).ConfigureAwait(false);
                    property.SetValue(result, value);
                }

                anyRecordReaded = true;
            }

            return result;
        }

        public List<T> GetList<T>(string sql, bool usingNavigation = true) where T : class, IEquatable<T>, new()
        {
            var properties = TypeExtensions.GetWritableProperties<T>();
            var result = new List<T>();

            using SqlConnection connection = new SqlConnection(_connectionStringModel.Value.ConnectionString);
            using SqlCommand command = new SqlCommand(sql, connection);

            connection.Open();

            using SqlDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection);

            var listProperties = TypeExtensions.GetListProperties<T>();

            while (reader.Read())
            {
                var temp = new T();

                foreach (var property in properties)
                {
                    if (property.IsClassProperty())
                    {
                        if (property.IsPropertyGenericList())
                        {
                            var innerProperty = property.GetGenericArgument();

                            var obj = Activator.CreateInstance(innerProperty);

                            var list = listProperties.FirstOrDefault(c => c.GetType() == property.PropertyType);

                            var objProperties = obj.GetWritableProperties();

                            foreach (var prop in objProperties)
                            {
                                var val = reader.GetFieldValue<object>(reader.GetOrdinal(prop.Name));
                                prop.SetValue(obj, val);
                            }

                            list.Add(obj);

                            var tempList = Activator.CreateInstance(property.PropertyType) as IList;

                            foreach (var item in list)
                            {
                                var foreignKeyValue = item.GetForeignKeyValue();
                                var parentPrimaryKey = item.GetPrimaryKeyValueFromChildEntity(temp);

                                if (foreignKeyValue.Equals(parentPrimaryKey))
                                    tempList.Add(item);
                            }

                            property.SetValue(temp, tempList);

                        }
                        else
                        {
                            var obj = Activator.CreateInstance(property.PropertyType);

                            var objProperties = obj.GetWritableProperties();

                            foreach (var prop in objProperties)
                            {
                                var val = reader.GetFieldValue<object>(reader.GetOrdinal(prop.Name));
                                prop.SetValue(obj, val);
                            }

                            property.SetValue(result, obj);
                        }

                        continue;
                    }

                    var value = reader.GetFieldValue<object>(reader.GetOrdinal(property.Name));
                    property.SetValue(temp, value is DBNull ? default : value);
                }

                if (result.Contains(temp) && usingNavigation)
                {
                    result.Remove(temp);
                    result.Add(temp);
                    continue;
                }
                result.Add(temp);
            }
            return result;
        }

        public async Task<List<T>> GetListAsync<T>(string sql, bool usingNavigation = true) where T : class, IEquatable<T>, new()
        {
            var properties = TypeExtensions.GetWritableProperties<T>();
            var result = new List<T>();
            await using SqlConnection connection = new SqlConnection(_connectionStringModel.Value.ConnectionString);
            await using SqlCommand command = new SqlCommand(sql, connection);

            await connection.OpenAsync().ConfigureAwait(false);

            await using SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection).ConfigureAwait(false);

            var listProperties = TypeExtensions.GetListProperties<T>();

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var temp = new T();

                foreach (var property in properties)
                {
                    if (property.IsClassProperty())
                    {
                        if (property.IsPropertyGenericList())
                        {
                            var innerProperty = property.GetGenericArgument();

                            var obj = Activator.CreateInstance(innerProperty);

                            var list = listProperties.FirstOrDefault(c => c.GetType() == property.PropertyType);

                            var objProperties = obj.GetWritableProperties();

                            foreach (var prop in objProperties)
                            {
                                var val = await reader.GetFieldValueAsync<object>(reader.GetOrdinal(prop.Name)).ConfigureAwait(false);
                                prop.SetValue(obj, val);
                            }

                            list.Add(obj);

                            var tempList = Activator.CreateInstance(property.PropertyType) as IList;

                            foreach (var item in list)
                            {
                                var foreignKeyValue = item.GetForeignKeyValue();
                                var parentPrimaryKey = item.GetPrimaryKeyValueFromChildEntity(temp);

                                if (foreignKeyValue.Equals(parentPrimaryKey))
                                    tempList.Add(item);
                            }

                            property.SetValue(temp, tempList);

                        }

                        else
                        {
                            var obj = Activator.CreateInstance(property.PropertyType);

                            var objProperties = obj.GetWritableProperties();

                            foreach (var prop in objProperties)
                            {
                                var val = await reader.GetFieldValueAsync<object>(reader.GetOrdinal(prop.Name));
                                prop.SetValue(obj, val);
                            }

                            property.SetValue(result, obj);
                        }

                        continue;
                    }

                    var value = await reader.GetFieldValueAsync<object>(reader.GetOrdinal(property.Name)).ConfigureAwait(false);

                    property.SetValue(temp, value is DBNull ? default : value);
                }

                if (result.Contains(temp) && usingNavigation)
                {
                    result.Remove(temp);
                    result.Add(temp);
                    continue;
                }



                result.Add(temp);
            }

            return result;
        }
    }
}