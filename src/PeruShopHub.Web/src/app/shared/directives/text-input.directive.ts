import { Directive, HostBinding } from '@angular/core';

@Directive({
  selector: 'input[appTextInput], textarea[appTextInput]',
  standalone: true,
})
export class TextInputDirective {
  @HostBinding('class.text-input') readonly hostClass = true;
}
