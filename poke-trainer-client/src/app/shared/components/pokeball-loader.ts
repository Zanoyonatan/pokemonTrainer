import { Component, Input } from '@angular/core';
@Component({ selector: 'app-pokeball-loader', templateUrl: './pokeball-loader.html', styleUrl: './pokeball-loader.scss' })
export class PokeballLoader { @Input() text = 'Loading...'; }
