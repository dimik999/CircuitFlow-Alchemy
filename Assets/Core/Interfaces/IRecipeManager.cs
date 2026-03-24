using System.Collections.Generic;
using CircuitFlowAlchemy.Core.Models;

namespace CircuitFlowAlchemy.Core.Interfaces
{
    /// <summary>
    /// Менеджер рецептов производства
    /// </summary>
    public interface IRecipeManager
    {
        /// <summary>
        /// Получить рецепт по ID
        /// </summary>
        Recipe GetRecipe(string recipeId);
        
        /// <summary>
        /// Получить все доступные рецепты
        /// </summary>
        IEnumerable<Recipe> GetAllRecipes();
        
        /// <summary>
        /// Проверить, можно ли выполнить рецепт с текущими ресурсами
        /// </summary>
        bool CanExecuteRecipe(string recipeId, IResourceService resourceService);
        
        /// <summary>
        /// Выполнить рецепт (потратить ресурсы и вернуть результат)
        /// </summary>
        Essence ExecuteRecipe(string recipeId, IResourceService resourceService);
    }
}
