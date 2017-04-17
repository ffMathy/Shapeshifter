/* tslint:disable:no-unused-variable */
import { TestBed, async } from '@angular/core/testing';
import { AppComponent } from './app.component';
describe('AppComponent', function () {
    beforeEach(function () {
        TestBed.configureTestingModule({
            declarations: [
                AppComponent
            ],
        });
        TestBed.compileComponents();
    });
    it('should create the app', async(function () {
        var fixture = TestBed.createComponent(AppComponent);
        var app = fixture.debugElement.componentInstance;
        expect(app).toBeTruthy();
    }));
    it("should have as title 'app works!'", async(function () {
        var fixture = TestBed.createComponent(AppComponent);
        var app = fixture.debugElement.componentInstance;
        expect(app.title).toEqual('app works!');
    }));
    it('should render title in a h1 tag', async(function () {
        var fixture = TestBed.createComponent(AppComponent);
        fixture.detectChanges();
        var compiled = fixture.debugElement.nativeElement;
        expect(compiled.querySelector('h1').textContent).toContain('app works!');
    }));
});
//# sourceMappingURL=C:/Users/mathi/Source/Repos/Shapeshifter/src/Shapeshifter.Website/wwwroot/Client/app/app.component.spec.js.map