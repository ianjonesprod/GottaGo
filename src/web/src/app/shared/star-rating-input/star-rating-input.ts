import { ChangeDetectionStrategy, Component, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatRadioModule } from '@angular/material/radio';

/**
 * A one-to-five rating input.
 *
 * Built on real radio buttons inside a fieldset, not a row of clickable divs. That is the
 * whole point: radios already give arrow-key navigation, a single tab stop for the group,
 * correct announcement of "3 of 5 selected", and they work with speech input because each
 * option has a real text name. Rebuilding that on divs means reimplementing all of it by
 * hand and getting some of it wrong.
 *
 * The stars are styled labels. The text is still there for assistive technology, it is just
 * not drawn.
 *
 * Nothing is selected by default. Pre-filling a 3 would record an opinion the person never
 * expressed.
 */
@Component({
  selector: 'gg-star-rating-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatRadioModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => StarRatingInput),
      multi: true,
    },
  ],
  templateUrl: './star-rating-input.html',
  styleUrl: './star-rating-input.scss',
})
export class StarRatingInput implements ControlValueAccessor {
  readonly label = input.required<string>();
  readonly describedBy = input<string | null>(null);

  protected readonly value = signal<number | null>(null);
  protected readonly disabled = signal(false);

  /** Wording each star announces, so "4" is not just a number without meaning. */
  protected readonly options = [
    { value: 1, text: '1 star, terrible' },
    { value: 2, text: '2 stars, poor' },
    { value: 3, text: '3 stars, fine' },
    { value: 4, text: '4 stars, good' },
    { value: 5, text: '5 stars, excellent' },
  ];

  private onChange: (value: number | null) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  writeValue(value: number | null): void {
    this.value.set(value);
  }

  registerOnChange(fn: (value: number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected select(value: number): void {
    this.value.set(value);
    this.onChange(value);
    this.onTouched();
  }
}
