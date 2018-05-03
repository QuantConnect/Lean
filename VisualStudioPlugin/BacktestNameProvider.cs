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
using System;
using System.Linq;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Static class used for generating random backtests names
    /// </summary>
    internal static class BacktestNameProvider
    {
        private static readonly string[] _animals = { "Horse", "Zebra", "Whale", "Tapir", "Barracuda", "Cow", "Cat",
           "Wolf", "Hamster", "Monkey", "Pelican", "Snake", "Albatross",
           "Viper", "Guanaco", "Anguilline", "Badger", "Dogfish", "Duck",
           "Butterfly", "Gaur", "Rat", "Termite", "Eagle", "Dinosaur",
           "Pig", "Seahorse", "Hornet", "Koala", "Hippopotamus",
           "Cormorant", "Jackal", "Rhinoceros", "Panda", "Elephant",
           "Penguin", "Beaver", "Hyena", "Parrot", "Crocodile", "Baboon",
           "Pony", "Chinchilla", "Fox", "Lion", "Mosquito", "Cobra", "Mule",
           "Coyote", "Alligator", "Pigeon", "Antelope", "Goat", "Falcon",
           "Owlet", "Llama", "Gull", "Chicken", "Caterpillar", "Giraffe",
           "Rabbit", "Flamingo", "Caribou", "Goshawk", "Galago", "Bee",
           "Jellyfish", "Buffalo", "Salmon", "Bison", "Dolphin", "Jaguar",
           "Dog", "Armadillo", "Gorilla", "Alpaca", "Kangaroo", "Dragonfly",
           "Salamander", "Owl", "Bat", "Sheep", "Frog", "Chimpanzee",
           "Bull", "Scorpion", "Lemur", "Camel", "Leopard", "Fish", "Donkey",
           "Manatee", "Shark", "Bear", "kitten", "Fly", "Ant", "Sardine"};
        private static readonly string[] _colors = { "Red", "Red-Orange", "Orange", "Yellow", "Tan", "Yellow-Green",
           "Yellow-Green", "Fluorescent Orange", "Apricot", "Green",
           "Fluorescent Pink", "Sky Blue", "Fluorescent Yellow", "Asparagus",
           "Blue", "Violet", "Light Brown", "Brown", "Magenta", "Black"};
        private static readonly string[] _verbs = { "Determined", "Pensive", "Adaptable", "Calculating", "Logical",
           "Energetic", "Creative", "Smooth", "Calm", "Hyper-Active",
           "Measured", "Fat", "Emotional", "Crying", "Jumping",
           "Swimming", "Crawling", "Dancing", "Focused", "Well Dressed",
           "Retrospective", "Hipster", "Square", "Upgraded", "Ugly",
           "Casual", "Formal", "Geeky", "Virtual", "Muscular",
           "Alert", "Sleepy" };
        private static readonly Random _random = new Random();

        /// <summary>
        /// Generates a new random backtest name with the format: verb + color + animal
        /// </summary>
        /// <returns>Backtest name</returns>
        public static string GetNewName()
        {
            var numberOfAnimals = _animals.Count();
            var numberOfVerbs = _verbs.Count();
            var numberOfColors = _colors.Count();

            int animalIndex = _random.Next(0, numberOfAnimals - 1);
            int verbIndex = _random.Next(0, numberOfVerbs - 1);
            int colorIndex = _random.Next(0, numberOfColors - 1);
            return _verbs[verbIndex] + " " + _colors[colorIndex] + " " + _animals[animalIndex];
        }
    }
}
