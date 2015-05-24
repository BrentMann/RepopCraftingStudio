﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace RePopCraftingStudio.Db
{
   // Jim's request for credit:
   // I want to be referenced as "The dude that was once too stoned to make Mac & CHeese"
   // Misugumi, Towan Navarr, Treyonna Sullivan

   public class RepopDb
   {
      //private string _connStr = string.Empty;
      public string ConnectionString { get; set; }

      public RepopDb()
         : this( string.Empty )
      {
      }

      public RepopDb( string connectionString )
      {
         ConnectionString = connectionString;
      }

      public string GetComponentName( EntityWithComponentId entity )
      {
         return GetComponentName( entity.ComponentId );
      }

      public string GetComponentName( long componentId )
      {
         return
            (string)
            GetDataRow( @"select displayName from crafting_components where componentId = {0}", componentId )
               .ItemArray[ 0 ];
      }

      public string GetRecipeName( RecipeResult recipeResult )
      {
         return
            (string)
            GetDataRow( @"select displayName from recipes where recipeId = {0}", recipeResult.RecipeId ).ItemArray[ 0 ];
      }

      public string GetCraftingFilterName( long filterId )
      {
         if ( 0 == filterId )
            return string.Empty;
         return
            (string)GetDataRow( @"select displayName from crafting_filters where filterId = {0}", filterId ).ItemArray[ 0 ];
      }

      public string GetItemName( long itemId )
      {
         if ( 0 == itemId )
            return string.Empty;
         return (string)GetDataRow( @"select displayName from items where itemId = {0}", itemId ).ItemArray[ 0 ];
      }

      public string GetSkillName( long skillId )
      {
         if ( 0 == skillId )
            return string.Empty;
         return (string)GetDataRow( @"select displayName from skills where skillId = {0}", skillId ).ItemArray[ 0 ];
      }

      public Item SelectItemById( long itemId )
      {
         return new Item( this, GetDataRow( @"select * from items where itemId = {0}", itemId ).ItemArray );
      }

      public IEnumerable<Item> SelectItemsByIds( IEnumerable<long> itemsIds )
      {
         IList<Item> items = new List<Item>();
         foreach ( long id in itemsIds )
         {
            items.Add( SelectItemById( id ) );
         }
         return items;
      }

      public CraftingComponent SelectCraftingComponentById( long componentId )
      {
         return new CraftingComponent( this, GetDataRow( @"select * from crafting_components where componentId = {0}", componentId ).ItemArray );
      }

      public IEnumerable<Item> SelectItemsByName( string filter )
      {
         return RowsToEntities(
            GetDataRows( @"select * from items where displayName like '%{0}%'", filter ),
            r => new Item( this, r.ItemArray ) );
      }

      public IEnumerable<Recipe> SelectRecipesForItem( Item item )
      {
         return SelectRecipesForItem( item.ItemId );
      }

      public IEnumerable<Recipe> SelectRecipesForItem( long itemId )
      {
         return RowsToEntities(
            GetDataRows(
               @"select * from recipes where recipeId in (select recipeId from recipe_results where resultId = {0})",
               itemId ),
            r => new Recipe( this, r.ItemArray ) );
      }

      public IEnumerable<RecipeAgent> SelectRecipeAgentsForRecipe( Recipe recipe )
      {
         return SelectRecipeAgentsForRecipe( recipe.RecipeId );
      }

      public IEnumerable<RecipeAgent> SelectRecipeAgentsForRecipe( long recipeId )
      {
         return RowsToEntities(
            GetDataRows(
               @"select * from recipe_agents where recipeId = {0}", recipeId ),
            r => new RecipeAgent( this, r.ItemArray ) );
      }

      public IEnumerable<RecipeIngredient> SelectRecipeIngredientsForRecipe( Recipe recipe )
      {
         return SelectRecipeIngredientsForRecipe( recipe.RecipeId );
      }

      public IEnumerable<RecipeIngredient> SelectRecipeIngredientsForRecipe( long recipeId )
      {
         return RowsToEntities(
            GetDataRows(
               @"select * from recipe_ingredients where recipeId = {0}", recipeId ),
            r => new RecipeIngredient( this, r.ItemArray ) );
      }

      public IEnumerable<RecipeResult> SelectRecipeResultsForRecipe( Recipe recipe )
      {
         return SelectRecipeResultsForRecipe( recipe.RecipeId );
      }

      public IEnumerable<RecipeResult> SelectRecipeResultsForRecipe( long recipeId )
      {
         return RowsToEntities(
            GetDataRows(
               @"select * from recipe_results where recipeId = {0}", recipeId ),
            r => new RecipeResult( this, r.ItemArray ) );
      }

      public IEnumerable<RecipeResult> SelectRecipeResultsForItem( long recipeId, long groupId )
      {
         return RowsToEntities(
            GetDataRows(
               @"select * from recipe_results where resultId = {0} and groupId = {1}", recipeId, groupId ),
            r => new RecipeResult( this, r.ItemArray ) );
      }

      public long[] SelectComponentIdsForRecipeIngredients( long recipeId )
      {
         // TODO: Jim, can this be reduced?
         IList<long> componentIds = new List<long>();

         for ( int slot = 1; slot <= 4; slot++ )
         {
            var rows = GetDataRows( @"select componentId from recipe_ingredients where recipeId={0} and ingSlot={1}",
                                   recipeId, slot );
            if ( 0 == rows.Count )
               break;

            componentIds.Add( (long)rows[ 0 ].ItemArray[ 0 ] );
         }

         return componentIds.ToArray();
      }

      public long SelectItemIdForCraftingComponentId( long componentId )
      {
         return (long)GetDataRow( @"select itemId from item_crafting_components where componentId = {0}", componentId ).ItemArray[ 0 ];
      }

      public IEnumerable<long> SelectItemIdsForCraftingComponentId( long componentId )
      {
         IList<long> itemIds = new List<long>();
         var rows = GetDataRows( @"select itemId from item_crafting_components where componentId={0} ", componentId );
         foreach ( DataRow row in rows )
         {
            itemIds.Add( (long)row.ItemArray[ 0 ] );
         }

         return itemIds;
      }

      public IEnumerable<long> SelectItemIdsForCraftingComponentId( long componentId, long filterId )
      {
         IList<long> itemIds = new List<long>();
         var rows = GetDataRows(
            "select * from item_crafting_filters " +
            "inner join item_crafting_components ON (item_crafting_components.itemId = item_crafting_filters.itemId) " +
            "where item_crafting_components.componentId = {0} and item_crafting_filters.filterId ={1}",
            componentId, filterId );
         foreach ( DataRow row in rows )
         {
            itemIds.Add( (long)row.ItemArray[ 0 ] );
         }

         return itemIds;
      }

      public IEnumerable<long> SelectComponentIdsForRecipeAgents( long recipeId )
      {
         IList<long> componentIds = new List<long>();
         var rows = GetDataRows( @"select componentId from recipe_agents where recipeId={0} ", recipeId );
         foreach ( DataRow row in rows )
         {
            componentIds.Add( (long)row.ItemArray[ 0 ] );
         }

         return componentIds;
      }

      // =============================================================================================

      private IEnumerable<T> RowsToEntities<T>( DataRowCollection rows, Func<DataRow, T> make ) where T : Entity
      {
         IList<T> list = new List<T>();

         foreach ( DataRow row in rows )
         {
            list.Add( make( row ) );
         }
         return list;
      }

      private DataRow GetDataRow( string format, params object[] args )
      {
         return GetDataRows( string.Format( format, args ) )[ 0 ];
      }

      private DataRowCollection GetDataRows( string format, params object[] args )
      {
         return GetDataTable( string.Format( format, args ) ).Rows;
      }

      private DataTable GetDataTable( string format, params object[] args )
      {
         string sql = string.Empty;

         try
         {
            sql = string.Format( format, args );
            Debug.WriteLine( sql );
            if ( string.IsNullOrEmpty( ConnectionString ) )
               throw new InvalidOperationException( @"Connection string is null or empty." );

            DataTable table = new DataTable();

            using ( SQLiteConnection connection = new SQLiteConnection( ConnectionString ) )
            {
               connection.Open();
               using ( SQLiteCommand command = new SQLiteCommand( connection ) )
               {
                  command.CommandText = sql;
                  SQLiteDataReader reader = command.ExecuteReader();
                  table.Load( reader );
                  return table;
               }
            }
         }
         catch ( Exception ex )
         {
            throw new InvalidOperationException( string.Format( "Error executing SQL statement.\nSQL:\n\n{0}", sql ), ex );
         }
      }

      public ItemManifest BuildManifest( long itemId )
      {
         LogInfo( @"Building manifest for item id: {0}.", itemId );

         IList<ManifestLineItem> ingredients = new List<ManifestLineItem>();
         IList<ManifestLineItem> agents = new List<ManifestLineItem>();

         // Get recipe results where resultId = itemId && groupId = 1
         IEnumerable<RecipeResult> recipeResults = SelectRecipeResultsForItem( itemId, 1 );
         LogInfo( @"Selected {0} recipe results.", recipeResults.Count() );

         // NOTE: will need to address multipe results next, for now use first recipe result
         RecipeResult recipeResult = recipeResults.First();

         // Get each ingredient entry component Id
         long[] componentIds = SelectComponentIdsForRecipeIngredients( recipeResult.RecipeId );
         LogInfo( @"Selected {0} ingredient component ids for recipe id: {1}.", componentIds.Count(), recipeResult.RecipeId );
         for ( int ingSlot = 1; ingSlot <= 4; ingSlot++ )
         {
            if ( componentIds.Length == ingSlot - 1 )
               break;

            long filterId = recipeResult.GetFilterId( ingSlot );
            long componentId = componentIds[ ingSlot - 1 ];

            if ( 0 == filterId )
            {
               long ingItemId = SelectItemIdForCraftingComponentId( componentId );
               LogInfo( @"Selected item id: {0} for compontent Id: {1}", ingItemId, componentId );
               ingredients.Add( new ManifestLineItem( this, componentId, ingItemId ) );
            }
            else
            {
               IEnumerable<long> ingItemIds = SelectItemIdsForCraftingComponentId( componentId, filterId );
               LogInfo( @"Selected item ids: {0} for compontent Id: {1}", ingItemIds.ToArray(), componentId );
               ingredients.Add( new ManifestLineItem( this, componentId, ingItemIds.ToArray() ) );
            }
         }

         // get each agent entry component Id
         // NOTE:  Brent, make attempt to write JOIN'd SQL for this in the morning.
         IEnumerable<long> compIds = SelectComponentIdsForRecipeAgents( recipeResult.RecipeId );
         LogInfo( @"Selected {0} agent component ids for recipe id: {1}.", compIds.Count(), recipeResult.RecipeId );
         foreach ( long compId in compIds )
         {
            agents.Add( new ManifestLineItem( this, compId, SelectItemIdsForCraftingComponentId( compId ) ) );
         }


         // Build up item manifest

         IList<ItemManifest.Entry> ingredientEntries = new List<ItemManifest.Entry>();
         foreach ( ManifestLineItem item in ingredients )
         {
            ingredientEntries.Add( new ItemManifest.Entry(
               SelectCraftingComponentById( item.ComponentId ),
               SelectItemsByIds( item.ItemIds )
               ) );
         }

         IList<ItemManifest.Entry> agentEntries = new List<ItemManifest.Entry>();
         foreach ( ManifestLineItem item in agents )
         {
            agentEntries.Add( new ItemManifest.Entry(
               SelectCraftingComponentById( item.ComponentId ),
               SelectItemsByIds( item.ItemIds )
               ) );
         }

         ItemManifest itemManifest = new ItemManifest
         {
            Item = SelectItemById( itemId ),
            Ingredients = ingredientEntries,
            Agents = agentEntries,
         };

         return itemManifest;
      }

      private void LogInfo( string format, params object[] args )
      {
         Debug.WriteLine( string.Format( format, args ) );
      }

      //public void SchemaTest()
      //{
      //   List<string> rawTables = new List<string>();
      //   var rows = GetDataRows( @"SELECT tbl_name FROM sqlite_master" );
      //   foreach ( DataRow row in rows )
      //   {
      //      rawTables.Add( (string)row.ItemArray[ 0 ] );
      //   }
      //   IEnumerable<string> tables = rawTables.OrderBy( t => t ).Distinct();

      //   XDocument doc = new XDocument( new XElement( @"Schema" ) );
      //   foreach ( string tableName in tables )
      //   {
      //      XElement ele = new XElement( tableName );
      //      DataTable table = GetDataTable( @"select * from {0}", tableName );
      //      foreach ( DataColumn column in table.Columns )
      //      {
      //         ele.Add( new XElement( column.ColumnName ) );
      //      }
      //      //string test = table.Columns[0].ColumnName;

      //      doc.Root.Add( ele );
      //   }

      //   string xml = doc.ToString();
      //}

      internal class ManifestLineItem
      {
         protected RepopDb Db { get; private set; }
         private readonly IEnumerable<long> _itemIds;
         private IList<string> _itemNames;

         public ManifestLineItem( RepopDb db, long componentId, long itemId )
            : this( db, componentId, new[] { itemId } )
         {

         }

         public ManifestLineItem( RepopDb db, long componentId, IEnumerable<long> itemIds )
         {
            Db = db;
            ComponentId = componentId;
            _itemIds = itemIds;
            GetItemNames();
         }

         public bool IsSpecific { get { return 1 == _itemIds.Count(); } }

         public long ComponentId { get; set; }
         public string ComponentName { get { return Db.GetComponentName( ComponentId ); } }

         public long ItemId
         {
            get { return 1 == _itemIds.Count() ? _itemIds.First() : 0; }
         }

         public IEnumerable<long> ItemIds { get { return _itemIds; } }
         public IEnumerable<string> ItemNames { get { return _itemNames; } }

         private void GetItemNames()
         {
            _itemNames = new List<string>();
            foreach ( long itemId in ItemIds )
            {
               _itemNames.Add( Db.GetItemName( itemId ) );
            }
         }
      }
   }

   public class ItemManifest
   {
      [DebuggerDisplay(@"{Component}")]
      public class Entry
      {
         public CraftingComponent Component { get; private set; }
         public IEnumerable<Item> Items { get; private set; }
         public bool IsSpecific { get { return 1 == Items.Count(); } }
         public Item SpecificItem { get { return Items.FirstOrDefault(); } }

         public Entry( CraftingComponent component, IEnumerable<Item> items )
         {
            Component = component;
            Items = items;
         }
      }

      public Item Item { get; set; }
      public IEnumerable<Entry> Ingredients { get; set; }
      public IEnumerable<Entry> Agents { get; set; }
   }
}