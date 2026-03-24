using System.Collections.Generic;
using System.Linq;
using CircuitFlowAlchemy.Core.Interfaces;
using CircuitFlowAlchemy.Core.Models;

namespace CircuitFlowAlchemy.Features.Production
{
    /// <summary>
    /// Реализация менеджера рецептов
    /// </summary>
    public class RecipeManager : IRecipeManager
    {
        private readonly Dictionary<string, Recipe> _recipes = new();
        
        public RecipeManager()
        {
            InitializeDefaultRecipes();
        }
        
        public Recipe GetRecipe(string recipeId)
        {
            return _recipes.ContainsKey(recipeId) ? _recipes[recipeId] : null;
        }
        
        public IEnumerable<Recipe> GetAllRecipes()
        {
            return _recipes.Values;
        }
        
        public bool CanExecuteRecipe(string recipeId, IResourceService resourceService)
        {
            var recipe = GetRecipe(recipeId);
            if (recipe == null)
                return false;
            
            return recipe.InputEssences.All(input => 
                resourceService.HasEnoughEssence(input.Key, input.Value));
        }
        
        public Essence ExecuteRecipe(string recipeId, IResourceService resourceService)
        {
            var recipe = GetRecipe(recipeId);
            if (recipe == null)
                return null;
            
            if (!CanExecuteRecipe(recipeId, resourceService))
                return null;
            
            // Потратить входные ресурсы
            foreach (var input in recipe.InputEssences)
            {
                resourceService.ConsumeEssence(input.Key, input.Value);
            }
            
            // Вернуть результат
            return new Essence(recipe.OutputEssence, recipe.OutputAmount, 1.0f);
        }
        
        private void InitializeDefaultRecipes()
        {
            // Вапор = Игнис + Аква
            var vaporRecipe = new Recipe
            {
                Id = "vapor",
                Name = "Вапор (Пар)",
                OutputEssence = EssenceType.Aeris, // Временно, пока нет составных типов
                OutputAmount = 1.0f,
                ProductionTime = 5.0f,
                RequiredPurity = 0.5f
            };
            vaporRecipe.InputEssences[EssenceType.Ignis] = 1.0f;
            vaporRecipe.InputEssences[EssenceType.Aqua] = 1.0f;
            _recipes["vapor"] = vaporRecipe;
            
            // Магма = Игнис + Терра
            var magmaRecipe = new Recipe
            {
                Id = "magma",
                Name = "Магма",
                OutputEssence = EssenceType.Ignis,
                OutputAmount = 1.5f,
                ProductionTime = 8.0f,
                RequiredPurity = 0.6f
            };
            magmaRecipe.InputEssences[EssenceType.Ignis] = 1.0f;
            magmaRecipe.InputEssences[EssenceType.Terra] = 1.0f;
            _recipes["magma"] = magmaRecipe;
        }
    }
}
