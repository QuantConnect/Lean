/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System.Linq;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Provides extension methods for alpha models
    /// </summary>
    public static class AlphaModelExtensions
    {
        /// <summary>
        /// Gets the name of the alpha model
        /// </summary>
        public static string GetModelName(this IAlphaModel model)
        {
            var namedModel = model as INamedModel;
            if (namedModel != null)
            {
                return namedModel.Name;
            }

            return model.GetType().Name;
        }


        /// <summary>
        /// Gets the alpha model with the provided name. If multiple models are found with the requested
        /// name then an error is thrown. Each alpha model needs to have its own unique name if it will be
        /// used by this function and other types that depend on the source model to identify the alpha model.
        /// </summary>
        /// <param name="model">The alpha model to check. This should likely be a <seealso cref="CompositeAlphaModel"/></param>
        /// <param name="sourceModel">The requested source alpha model name</param>
        /// <returns>The matching source alpha model</returns>
        public static IAlphaModel GetByName(this IAlphaModel model, string sourceModel)
        {
            var modelName = model.GetModelName();
            if (modelName == sourceModel)
            {
                return model;
            }

            var composite = model as CompositeAlphaModel;
            if (composite == null)
            {
                throw new IncompatibleFrameworkModelsException(
                    modelName,
                    $"The configured models require the {nameof(CompositeAlphaModel)}. " +
                    "Please use the 'AddAlpha' method instead of 'SetAlpha' to configure multiple alpha models."
                );
            }

            var alphaModels = composite.Where(alpha => alpha.GetModelName() == sourceModel).ToList();
            if (alphaModels.Count != 1)
            {
                throw new IncompatibleFrameworkModelsException(
                    modelName,
                    $"{modelName} found {alphaModels.Count} alpha models matching sourceModel='{sourceModel}'." +
                    "The configured models require exactly one matching alpha model."
                );
            }

            return alphaModels[0];
        }
    }
}